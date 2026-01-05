using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Duckov;
using Duckov.Buffs;
using Duckov.UI;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.MonoBehaviours.Animators;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Utils;
using FMOD.Studio;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelHandler : MonoBehaviour
    {
        public const string CustomModelInstanceName = "CustomModelInstance";

        private const int PriorityNormal = 0; // 普通设置（Character, Pet等）或 AllAICharacters
        private const int PriorityAISpecific = 1; // AI单独设置（仅内建AI）

        private static readonly IReadOnlyDictionary<string, FieldInfo> OriginalModelSocketFieldInfos =
            CharacterModelSocketUtils.AllSocketFields;

        private readonly HashSet<GameObject> _currentUsingCustomSocketObjects = [];
        private readonly Dictionary<string, Transform> _customModelLocators = [];
        private readonly Dictionary<FieldInfo, Transform> _customModelSockets = [];
        private readonly HashSet<GameObject> _modifiedDeathLootBoxes = [];
        private readonly Dictionary<string, Transform> _originalModelLocators = [];
        private readonly Dictionary<FieldInfo, Transform> _originalModelSockets = [];

        private readonly Dictionary<string, List<EventInstance>> _playingSoundInstances = [];
        private readonly Dictionary<string, List<string>> _soundsByTag = [];
        private readonly Dictionary<string, float> _soundTagPlayChance = [];

        public readonly SortedDictionary<int, string> ModelPriorityList = [];
        private Renderer[]? _cachedCustomModelRenderers;

        private ModelBundleInfo? _currentModelBundleInfo;
        private CustomCharacterSoundMaker? _customCharacterSoundMaker;
        private CharacterSubVisuals? _customModelSubVisuals;
        private GameObject? _deathLootBoxPrefab;
        private GameObject? _headColliderObject;

        private float _nextIdleAudioTime;

        private bool ReplaceShader => CurrentModelInfo is not { Features: { Length: > 0 } }
                                      || !CurrentModelInfo.Features.Contains(ModelFeatures.NoAutoShaderReplace);

        public CharacterMainControl? CharacterMainControl { get; private set; }
        public CharacterModel? OriginalCharacterModel { get; private set; }
        public GameObject? OriginalModelOcclusionBody { get; private set; }
        public CharacterSoundMaker? OriginalCharacterSoundMaker { get; private set; }
        public CharacterAnimationControl? OriginalAnimationControl { get; private set; }
        public CharacterAnimationControl_MagicBlend? OriginalMagicBlendAnimationControl { get; private set; }
        public Movement? OriginalMovement { get; private set; }
        public bool IsHiddenOriginalModel { get; private set; }

        public CharacterBuffManager? BuffManager =>
            CharacterMainControl != null ? CharacterMainControl.GetBuffManager() : null;

        public ReadOnlyCollection<Buff> Buffs =>
            BuffManager != null ? BuffManager.Buffs : new ReadOnlyCollection<Buff>([]);

        public bool IsHiddenOriginalEquipment
        {
            get
            {
                if (OriginalCharacterModel == null) return false;
                if (ModEntry.HideEquipmentConfig == null) return false;
                if (!IsHiddenOriginalModel || CustomModelInstance == null) return false;

                if (!ModelTargetType.IsAICharacterTargetType(TargetTypeId))
                    return ModEntry.HideEquipmentConfig.GetHideEquipment(TargetTypeId);
                var nameKey = CharacterMainControl?.characterPreset?.nameKey;
                if (string.IsNullOrEmpty(nameKey))
                    return ModEntry.HideEquipmentConfig.GetHideEquipment(TargetTypeId);
                var effectiveNameKey = GetEffectiveAICharacterConfigKey(nameKey);
                var effectiveTargetTypeId = ModelTargetType.CreateAICharacterTargetType(effectiveNameKey);
                return ModEntry.HideEquipmentConfig.GetHideEquipment(effectiveTargetTypeId) ||
                       ModEntry.HideEquipmentConfig.GetHideEquipment(ModelTargetType.AllAICharacters);
            }
        }

        public bool IsModelAudioEnabled
        {
            get
            {
                var modelAudioConfig = ModEntry.ModelAudioConfig;
                return modelAudioConfig == null || modelAudioConfig.IsModelAudioEnabled(TargetTypeId);
            }
        }

        public float ModelAudioVolume
        {
            get
            {
                var modelAudioConfig = ModEntry.ModelAudioConfig;
                return modelAudioConfig?.GetModelAudioVolume(TargetTypeId) ?? 1f;
            }
        }

        public string TargetTypeId { get; private set; } = string.Empty;
        public string? NameKey => CharacterMainControl?.characterPreset?.nameKey;

        public string? CurrentModelDirectory => _currentModelBundleInfo?.DirectoryPath;

        public ModelInfo? CurrentModelInfo { get; private set; }

        public bool IsInitialized { get; private set; }

        public GameObject? CustomModelInstance { get; private set; }
        public Animator? CustomAnimator { get; private set; }
        public CustomAnimatorControl? CustomAnimatorControl { get; private set; }

        private void Update()
        {
            if (!IsInitialized || CharacterMainControl == null) return;
            if (!HasIdleSounds()) return;
            if (CharacterMainControl.Health != null && CharacterMainControl.Health.IsDead) return;

            if (ModEntry.IdleAudioConfig != null)
                if (!ModEntry.IdleAudioConfig.IsIdleAudioEnabled(TargetTypeId))
                    return;

            if (!(Time.time >= _nextIdleAudioTime)) return;
            PlayIdleAudio();
            ScheduleNextIdleAudio();
        }

        private void LateUpdate()
        {
            RefreshPlayingSounds();

            if (CharacterMainControl == null || OriginalCharacterModel == null) return;

            var equipmentController = CharacterMainControl.EquipmentController;

            var equipmentAgents = new[]
            {
                GetSlotActiveAgent(equipmentController.armorSlot),
                GetSlotActiveAgent(equipmentController.helmatSlot),
                GetSlotActiveAgent(equipmentController.backpackSlot),
                GetSlotActiveAgent(equipmentController.faceMaskSlot),
                GetSlotActiveAgent(equipmentController.headsetSlot),
            };

            if (IsHiddenOriginalEquipment)
                foreach (var agent in equipmentAgents.OfType<ItemAgent>().Where(agent => agent.gameObject.activeSelf))
                    agent.gameObject.SetActive(false);
            else
                foreach (var agent in equipmentAgents.OfType<ItemAgent>().Where(agent => !agent.gameObject.activeSelf))
                    agent.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            ModelSoundTrigger.OnSoundTriggered -= OnSoundTriggered;
            ModelSoundStopTrigger.OnSoundStopTriggered -= OnSoundStopTriggered;

            Health.OnHurt -= OnGlobalHurt;
            Health.OnDead -= OnGlobalDead;


            ModelManager.UnregisterHandler(this);

            if (CharacterMainControl == null) return;
            if (CharacterMainControl.Health == null) return;
            CharacterMainControl.Health.OnHurtEvent.RemoveListener(OnHurt);
            CharacterMainControl.Health.OnDeadEvent.RemoveListener(OnDeath);
        }

        public string GetTargetTypeId()
        {
            return TargetTypeId;
        }

        public void Initialize(CharacterMainControl characterMainControl, string targetTypeId)
        {
            if (IsInitialized) return;
            if (string.IsNullOrWhiteSpace(targetTypeId))
            {
                ModLogger.LogError("Target type ID is null or empty.");
                return;
            }

            CharacterMainControl = characterMainControl;
            TargetTypeId = targetTypeId;
            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl component not found.");
                return;
            }

            OriginalCharacterModel = CharacterMainControl.characterModel;
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("No CharacterModel found on CharacterMainControl.");
                return;
            }

            OriginalMovement = CharacterMainControl.movementControl;
            if (OriginalMovement == null)
            {
                ModLogger.LogError("No Movement component found on CharacterMainControl.");
                return;
            }

            OriginalAnimationControl = OriginalCharacterModel.GetComponent<CharacterAnimationControl>();
            OriginalMagicBlendAnimationControl =
                OriginalCharacterModel.GetComponent<CharacterAnimationControl_MagicBlend>();
            if (OriginalAnimationControl == null && OriginalMagicBlendAnimationControl == null)
                ModLogger.LogError("No CharacterAnimationControl component found on CharacterModel.");

            var customAnimatorControl = CharacterMainControl.GetComponent<CustomAnimatorControl>();
            if (customAnimatorControl == null)
                customAnimatorControl = CharacterMainControl.gameObject.AddComponent<CustomAnimatorControl>();

            CustomAnimatorControl = customAnimatorControl;
            customAnimatorControl.Initialize(this);

            RecordOriginalModelSockets();
            RecordOriginalModelOcclusionBody();
            RecordOriginalHeadCollider();
            RecordOriginalSoundMaker();

            if (CharacterMainControl.Health != null)
            {
                CharacterMainControl.Health.OnHurtEvent.AddListener(OnHurt);
                CharacterMainControl.Health.OnDeadEvent.AddListener(OnDeath);
            }

            Health.OnHurt += OnGlobalHurt;
            Health.OnDead += OnGlobalDead;

            ModelSoundTrigger.OnSoundTriggered += OnSoundTriggered;
            ModelSoundStopTrigger.OnSoundStopTriggered += OnSoundStopTriggered;


            InitializeModelPriorityList();

            ModelManager.RegisterHandler(this);

            ModLogger.Log("ModelHandler initialized successfully.");
            IsInitialized = true;
        }

        private void InitializeModelPriorityList()
        {
            if (ModEntry.UsingModel == null)
            {
                ModelPriorityList.Remove(PriorityNormal);
                ModelPriorityList.Remove(PriorityAISpecific);
                return;
            }

            var isAICharacter = ModelTargetType.IsAICharacterTargetType(TargetTypeId);

            string? modelID;
            if (isAICharacter)
            {
                modelID = ModEntry.UsingModel.GetModelID(ModelTargetType.AllAICharacters);

                var nameKey = ModelTargetType.ExtractAICharacterName(TargetTypeId);
                if (!string.IsNullOrEmpty(nameKey))
                {
                    var specificModelID = ModEntry.UsingModel.GetModelID(TargetTypeId);
                    if (!string.IsNullOrEmpty(specificModelID))
                        ModelPriorityList[PriorityAISpecific] = specificModelID;
                    else
                        ModelPriorityList.Remove(PriorityAISpecific);
                }
            }
            else
            {
                modelID = ModEntry.UsingModel.GetModelID(TargetTypeId);
                ModelPriorityList.Remove(PriorityAISpecific);
            }

            if (!string.IsNullOrEmpty(modelID))
                ModelPriorityList[PriorityNormal] = modelID;
            else
                ModelPriorityList.Remove(PriorityNormal);
        }

        public void UpdateModelPriorityList()
        {
            InitializeModelPriorityList();

            if (IsHiddenOriginalModel || CustomModelInstance != null)
                CleanupCustomModel();

            if (ModelPriorityList.Count == 0) return;

            foreach (var priority in ModelPriorityList.Keys.Reverse())
            {
                var modelID = ModelPriorityList[priority];
                if (string.IsNullOrEmpty(modelID)) continue;

                if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo)) continue;

                if (ModelTargetType.IsAICharacterTargetType(TargetTypeId))
                {
                    var nameKey = ModelTargetType.ExtractAICharacterName(TargetTypeId);
                    if (string.IsNullOrEmpty(nameKey)) continue;
                    if (!modelInfo.CompatibleWithAICharacter(nameKey)) continue;
                }
                else
                {
                    if (!modelInfo.CompatibleWithTargetType(TargetTypeId)) continue;
                }

                InitializeCustomModel(bundleInfo, modelInfo);
                return;
            }
        }

        public void InitializeFrom(ModelHandler sourceHandler, CharacterMainControl characterMainControl)
        {
            if (sourceHandler == null)
            {
                ModLogger.LogError("Source ModelHandler is null.");
                return;
            }

            if (!sourceHandler.IsInitialized || !sourceHandler.IsHiddenOriginalModel)
            {
                var targetTypeId = sourceHandler.GetTargetTypeId();
                Initialize(characterMainControl, targetTypeId);
                if (!IsInitialized) return;

                if (sourceHandler is not { _currentModelBundleInfo: not null, CurrentModelInfo: not null }) return;
                InitializeCustomModel(sourceHandler._currentModelBundleInfo, sourceHandler.CurrentModelInfo);
                if (CustomModelInstance != null) CustomModelInstance.SetActive(false);

                return;
            }

            if (characterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl component not found.");
                return;
            }

            var newCharacterModel = characterMainControl.characterModel;
            if (newCharacterModel == null)
            {
                ModLogger.LogError("No CharacterModel found on CharacterMainControl.");
                return;
            }

            RestoreSocketsFromSource(sourceHandler, newCharacterModel);

            var remnantCustomModel = newCharacterModel.transform.Find(CustomModelInstanceName);
            if (remnantCustomModel != null) DestroyImmediate(remnantCustomModel.gameObject);

            var targetTypeId2 = sourceHandler.GetTargetTypeId();
            Initialize(characterMainControl, targetTypeId2);
            if (!IsInitialized) return;
            if (CharacterMainControl == null || OriginalCharacterModel == null) return;

            if (sourceHandler is not { _currentModelBundleInfo: not null, CurrentModelInfo: not null }) return;
            InitializeCustomModel(sourceHandler._currentModelBundleInfo, sourceHandler.CurrentModelInfo);
        }

        private static void RestoreSocketsFromSource(ModelHandler sourceHandler, CharacterModel newCharacterModel)
        {
            if (sourceHandler.OriginalCharacterModel == null) return;

            var sourceRoot = sourceHandler.OriginalCharacterModel.transform;
            var myRoot = newCharacterModel.transform;

            foreach (var (socketField, originalSocketTransform) in sourceHandler._originalModelSockets)
            {
                if (originalSocketTransform == null) continue;

                var path = GetTransformPath(sourceRoot, originalSocketTransform);
                if (string.IsNullOrEmpty(path)) continue;

                var myOriginalSocket = myRoot.Find(path);
                if (myOriginalSocket == null)
                {
                    ModLogger.LogWarning($"[RestoreSocketsFromSource] Failed to find socket '{path}' in clone.");
                    continue;
                }

                var currentSocket = socketField.GetValue(newCharacterModel) as Transform;
                if (currentSocket == null || currentSocket == myOriginalSocket) continue;

                var children = currentSocket.OfType<Transform>().ToList();

                socketField.SetValue(newCharacterModel, myOriginalSocket);

                foreach (var child in children)
                {
                    child.SetParent(myOriginalSocket, false);
                    child.localRotation = Quaternion.identity;
                    child.localPosition = Vector3.zero;
                }
            }
        }

        private static string GetTransformPath(Transform root, Transform target)
        {
            if (target == root) return "";

            var path = target.name;
            var current = target.parent;

            while (current != null && current != root)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }

        public bool HaveCustomDeathLootBox()
        {
            return _deathLootBoxPrefab != null;
        }

        public GameObject? CreateCustomDeathLootBoxInstance()
        {
            if (_deathLootBoxPrefab == null) return null;
            var instance = Instantiate(_deathLootBoxPrefab);
            instance.name = "DeathLootBox_CustomModel";

            var renderers = GetAllRenderers(instance);
            ReplaceRenderersLayer(renderers);

            if (ReplaceShader)
                ReplaceRenderersShader(renderers);

            return instance;
        }

        public void RegisterCustomSocketObject(GameObject customSocketObject)
        {
            if (customSocketObject == null) return;
            _currentUsingCustomSocketObjects.Add(customSocketObject);
            UpdateToCustomSocket(customSocketObject);
        }

        public void UnregisterCustomSocketObject(GameObject customSocketObject, bool restore = true)
        {
            if (customSocketObject == null) return;
            _currentUsingCustomSocketObjects.Remove(customSocketObject);
            if (restore)
                RestoreCustomSocketObject(customSocketObject);
        }

        public void RegisterModifiedDeathLootBox(GameObject deathLootBox)
        {
            if (deathLootBox == null) return;
            _modifiedDeathLootBoxes.Add(deathLootBox);
        }

        public void UnregisterModifiedDeathLootBox(GameObject deathLootBox)
        {
            if (deathLootBox == null) return;
            _modifiedDeathLootBoxes.Remove(deathLootBox);
        }

        public Transform? GetOriginalSocketTransform(string socketName)
        {
            if (OriginalCharacterModel == null) return null;

            if (_originalModelLocators.TryGetValue(socketName, out var cachedLocator)
                && cachedLocator != null)
                return cachedLocator;

            return null;
        }

        public Transform? GetCustomSocketTransform(string socketName)
        {
            if (CustomModelInstance == null) return null;

            if (_customModelLocators.TryGetValue(socketName, out var cachedLocator)
                && cachedLocator != null)
                return cachedLocator;

            return null;
        }

        public void CleanupCustomModel()
        {
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is not set.");
                return;
            }

            if (IsHiddenOriginalModel)
            {
                if (_customModelSubVisuals != null)
                    CharacterMainControl.RemoveVisual(_customModelSubVisuals);

                RestoreToOriginalModelSockets();
                UpdateColliderHeight();

                if (OriginalCharacterSoundMaker != null)
                    OriginalCharacterSoundMaker.enabled = true;

                var customFaceInstance = GetOriginalCustomFaceInstance();
                if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(true);
                if (CustomModelInstance != null) CustomModelInstance.SetActive(false);

                ForceUpdateHealthBar();

                ModLogger.Log("Restored to original model.");
                IsHiddenOriginalModel = false;

                NotifyModelChanged(true);
            }

            CurrentModelInfo = null;
            _currentModelBundleInfo = null;

            if (CustomModelInstance != null)
            {
                CustomAnimator = null;

                DestroyImmediate(CustomModelInstance);
                CustomModelInstance = null;
            }

            _cachedCustomModelRenderers = null;

            if (CustomAnimatorControl != null)
                CustomAnimatorControl.SetCustomAnimator(null);

            if (_deathLootBoxPrefab != null)
            {
                DestroyImmediate(_deathLootBoxPrefab);
                _deathLootBoxPrefab = null;
            }

            foreach (var destroyAdapter in _modifiedDeathLootBoxes
                         .Select(deathLootBox => deathLootBox.GetComponent<OnDestroyAdapter>())
                         .Where(destroyAdapter => destroyAdapter != null))
            {
                destroyAdapter.ForceInvoke();
                destroyAdapter.ClearListeners();

                DestroyImmediate(destroyAdapter.gameObject);
            }

            _customModelLocators.Clear();
            _customModelSockets.Clear();
            _soundsByTag.Clear();
            IsHiddenOriginalModel = false;
        }

        public void InitializeCustomModel(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            if (IsHiddenOriginalModel || CustomModelInstance != null)
                CleanupCustomModel();

            var prefab = AssetBundleManager.LoadModelPrefab(modelBundleInfo, modelInfo);
            if (prefab == null)
            {
                ModLogger.LogError("Failed to load custom model prefab.");
                return;
            }

            _currentModelBundleInfo = modelBundleInfo;
            CurrentModelInfo = modelInfo;
            InitSoundFilePath(modelBundleInfo, modelInfo);
            InitializeDeathLootBoxPrefab(modelBundleInfo, modelInfo);
            InitializeCustomModelInternal(prefab, modelInfo);

            NotifyModelChanged(false);

            if (!HasIdleSounds()) return;
            if (ModEntry.IdleAudioConfig == null || ModEntry.IdleAudioConfig.IsIdleAudioEnabled(TargetTypeId))
                ScheduleNextIdleAudio();
        }

        private void InitializeDeathLootBoxPrefab(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            _deathLootBoxPrefab = null;

            if (string.IsNullOrWhiteSpace(modelInfo.DeathLootBoxPrefabPath)) return;

            var prefab = AssetBundleManager.LoadDeathLootBoxPrefab(modelBundleInfo, modelInfo);
            if (prefab == null) return;

            _deathLootBoxPrefab = prefab;
            ModLogger.Log($"Death loot box prefab initialized: {prefab.name}");
        }

        private void InitializeCustomModelInternal(GameObject customModelPrefab, ModelInfo modelInfo)
        {
            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is not set.");
                return;
            }

            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            if (CustomModelInstance != null) CleanupCustomModel();

            // Instantiate the custom model prefab
            CustomModelInstance = Instantiate(customModelPrefab, OriginalCharacterModel.transform);
            CustomModelInstance.name = CustomModelInstanceName;

            _cachedCustomModelRenderers = GetAllRenderers(CustomModelInstance);
            ReplaceRenderersLayer(_cachedCustomModelRenderers);
            if (ReplaceShader)
                ReplaceRenderersShader(_cachedCustomModelRenderers);

            SetShowBackMaterial();
            InitializeCustomCharacterSubVisuals();
            InitializeCustomCharacterSoundMaker(modelInfo);

            // Get the Animator component from the custom model
            CustomAnimator = CustomModelInstance.GetComponent<Animator>();
            if (CustomAnimatorControl != null)
                if (CustomAnimator != null)
                {
                    CustomAnimatorControl.SetCustomAnimator(CustomAnimator);
                }
                else
                {
                    ModLogger.LogError("No Animator component found on custom model instance.");
                    CustomAnimatorControl.SetCustomAnimator(null);
                }

            RecordCustomModelSockets();

            ModLogger.Log($"Custom model initialized: {customModelPrefab.name}");

            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is not set.");
                return;
            }

            if (_customModelSubVisuals != null)
                CharacterMainControl.AddSubVisuals(_customModelSubVisuals);

            ChangeToCustomModelSockets();
            UpdateColliderHeight();

            if (OriginalCharacterSoundMaker != null)
                OriginalCharacterSoundMaker.enabled = false;

            var customFaceInstance = GetOriginalCustomFaceInstance();
            if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(false);

            CustomModelInstance.SetActive(true);

            ForceUpdateHealthBar();
            OriginalCharacterModel.SyncHiddenToMainCharacter();

            if (!IsHiddenOriginalModel)
                ModLogger.Log("Changed to custom model.");
            IsHiddenOriginalModel = true;
        }

        private void RecordOriginalModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            _originalModelSockets.Clear();

            var socketFields = OriginalModelSocketFieldInfos.ToArray();
            foreach (var (socketName, socketField) in socketFields)
            {
                var socketTransform = socketField.GetValue(OriginalCharacterModel) as Transform;
                if (socketTransform == null) continue;
                _originalModelSockets[socketField] = socketTransform;
                _originalModelLocators[socketName] = socketTransform;
            }
        }

        private void RestoreToOriginalModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            RestoreCustomSocketObjects();
            foreach (var kvp in _originalModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
        }

        private void RecordCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            _customModelSockets.Clear();

            var socketFields = OriginalModelSocketFieldInfos.ToArray();
            foreach (var locatorName in SocketNames.InternalSocketNames)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, locatorName);
                if (locatorTransform == null) continue;
                _customModelLocators[locatorName] = locatorTransform;

                var fieldInfo = socketFields.FirstOrDefault(f => f.Key == locatorName).Value;
                if (fieldInfo != null)
                    _customModelSockets[fieldInfo] = locatorTransform;
            }

            foreach (var locatorName in SocketNames.ExternalSocketNames)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, locatorName);
                if (locatorTransform == null) continue;
                _customModelLocators[locatorName] = locatorTransform;
            }
        }

        private void ChangeToCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            RestoreToOriginalModelSockets();
            foreach (var kvp in _customModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
            UpdateCustomSocketObjects();
        }

        private Transform? GetOriginalCustomFaceInstance()
        {
            if (OriginalAnimationControl != null)
                return OriginalAnimationControl.animator.transform;
            return OriginalMagicBlendAnimationControl != null
                ? OriginalMagicBlendAnimationControl.animator.transform
                : null;
        }

        private void RecordOriginalModelOcclusionBody()
        {
            if (OriginalModelOcclusionBody != null) return;

            var originalCustomFaceInstance = GetOriginalCustomFaceInstance();
            if (originalCustomFaceInstance == null) return;

            var originalDuckBody = originalCustomFaceInstance.Find("DuckBody");
            if (originalDuckBody == null) return;

            OriginalModelOcclusionBody = originalDuckBody.gameObject;
        }

        private void RecordOriginalHeadCollider()
        {
            if (OriginalCharacterModel == null) return;

            var helmetTransform = GetOriginalSocketTransform(SocketNames.Helmet);
            if (helmetTransform == null) return;

            var headCollider = helmetTransform.GetComponentInChildren<HeadCollider>();
            if (headCollider == null) return;

            _headColliderObject = headCollider.gameObject;
        }

        private void RecordOriginalSoundMaker()
        {
            if (OriginalCharacterSoundMaker != null) return;

            if (CharacterMainControl == null) return;

            var soundMaker = CharacterMainControl.GetComponent<CharacterSoundMaker>();
            if (soundMaker == null) return;

            OriginalCharacterSoundMaker = soundMaker;
        }

        private void UpdateColliderHeight()
        {
            if (_headColliderObject == null || CharacterMainControl == null) return;

            var mainDamageReceiver = CharacterMainControl.mainDamageReceiver;
            if (mainDamageReceiver == null) return;

            var capsuleCollider = mainDamageReceiver.GetComponent<CapsuleCollider>();
            if (capsuleCollider == null) return;

            var height = (float)(_headColliderObject.transform.localScale.y * 0.5 +
                _headColliderObject.transform.position.y - CharacterMainControl.transform.position.y + 0.5);
            capsuleCollider.height = height;
            capsuleCollider.center = Vector3.up * (height * 0.5f);
        }

        private void UpdateToCustomSocket(GameObject targetGameObject)
        {
            if (OriginalCharacterModel == null || targetGameObject == null)
                return;

            var customSocketMarker = targetGameObject.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null) return;

            var customSocketNames = customSocketMarker.CustomSocketNames;
            foreach (var socketName in customSocketNames)
            {
                var customSocket = GetSocketTransform(socketName);
                if (customSocket == null) continue;
                targetGameObject.transform.SetParent(customSocket, false);
                targetGameObject.transform.localPosition = Vector3.zero;
                targetGameObject.transform.localRotation = Quaternion.identity;
                targetGameObject.transform.localScale = Vector3.one;
                return;
            }
        }

        private static ItemAgent? GetSlotActiveAgent(Slot? slot)
        {
            if (slot == null || slot.Content == null) return null;
            return slot.Content.ActiveAgent != null ? slot.Content.ActiveAgent : null;
        }

        private Transform? GetSocketTransform(string socketName)
        {
            if (OriginalCharacterModel == null) return null;

            if (_customModelLocators.TryGetValue(socketName, out var cachedLocator)
                && cachedLocator != null)
                return cachedLocator;

            if (_originalModelLocators.TryGetValue(socketName, out var originalLocator)
                && originalLocator != null)
                return originalLocator;

            return null;
        }

        private void UpdateCustomSocketObjects()
        {
            if (OriginalCharacterModel == null || CustomModelInstance == null) return;

            var targets = _currentUsingCustomSocketObjects.ToArray();
            _currentUsingCustomSocketObjects.Clear();
            foreach (var customSocketObject in targets)
            {
                if (customSocketObject == null) continue;
                _currentUsingCustomSocketObjects.Add(customSocketObject);
                UpdateToCustomSocket(customSocketObject);
            }
        }

        private void RestoreCustomSocketObjects()
        {
            if (OriginalCharacterModel == null) return;

            foreach (var customSocketObject in _currentUsingCustomSocketObjects.ToArray())
            {
                if (customSocketObject == null)
                {
                    _currentUsingCustomSocketObjects.Remove(customSocketObject!);
                    continue;
                }

                RestoreCustomSocketObject(customSocketObject);
            }
        }

        private void RestoreCustomSocketObject(GameObject customSocketObject)
        {
            if (OriginalCharacterModel == null || customSocketObject == null) return;

            var customSocketMarker = customSocketObject.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null || customSocketMarker.OriginParent == null) return;

            customSocketObject.transform.SetParent(customSocketMarker.OriginParent, false);
            customSocketObject.transform.localPosition = customSocketMarker.SocketOffset ?? Vector3.zero;
            customSocketObject.transform.localRotation = customSocketMarker.SocketRotation ?? Quaternion.identity;
            customSocketObject.transform.localScale = customSocketMarker.SocketScale ?? Vector3.one;
        }

        private void ReplaceModelSocket(FieldInfo socketField, Transform? newSocket)
        {
            if (OriginalCharacterModel == null || newSocket == null) return;
            var originalSocket = socketField.GetValue(OriginalCharacterModel) as Transform;
            if (originalSocket == newSocket) return;
            var originalChildren = new List<Transform>();
            if (originalSocket != null)
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (Transform child in originalSocket)
                    if (child != null)
                        originalChildren.Add(child);
            socketField.SetValue(OriginalCharacterModel, newSocket);
            foreach (var child in originalChildren)
            {
                child.SetParent(newSocket, false);
                child.localRotation = Quaternion.identity;
                child.localPosition = Vector3.zero;
            }
        }

        private void SetShowBackMaterial()
        {
            if (CurrentModelInfo is { Features.Length: > 0 }
                && CurrentModelInfo.Features.Contains(ModelFeatures.SkipShowBackMaterial))
                return;

            if (OriginalModelOcclusionBody == null) return;

            var originalSkinnedMeshRenderer =
                OriginalModelOcclusionBody.GetComponent<SkinnedMeshRenderer>();
            if (originalSkinnedMeshRenderer == null) return;

            var characterShowBackShader = GameCharacterShowBackShader;
            if (characterShowBackShader == null) return;

            var originalMaterial = originalSkinnedMeshRenderer.materials
                .FirstOrDefault(m => m != null && m.shader == characterShowBackShader);
            if (originalMaterial == null) return;

            var clonedMaterial = new Material(originalMaterial);
            var renderers = _cachedCustomModelRenderers?.OfType<SkinnedMeshRenderer>().ToArray();
            if (renderers == null || renderers.Length == 0) return;
            foreach (var renderer in renderers)
            {
                if (renderer.material == null) continue;
                if (renderer.materials.Any(m => m != null && m.shader == characterShowBackShader))
                    continue;
                renderer.materials = renderer.materials.Concat([clonedMaterial]).ToArray();
            }
        }

        private void ForceUpdateHealthBar()
        {
            if (CharacterMainControl == null || CharacterMainControl.Health == null) return;
            var healthBar = HealthBarManager.Instance.GetActiveHealthBar(CharacterMainControl.Health);
            if (healthBar == null) return;
            healthBar.RefreshOffset();
        }

        private void OnHurt(DamageInfo damageInfo)
        {
            var isCrit = damageInfo.crit > 0;

            if (CustomAnimatorControl != null)
            {
                if (isCrit)
                    CustomAnimatorControl.TriggerCritHurt();
                else
                    CustomAnimatorControl.TriggerHurt();
            }

            if (!IsModelAudioEnabled) return;

            string soundTag;
            string eventName;
            if (isCrit)
            {
                soundTag = SoundTags.TriggerOnCritHurt;
                eventName = "onCritHurt";
            }
            else
            {
                soundTag = SoundTags.TriggerOnHurt;
                eventName = "onHurt";
            }

            var soundPath = GetRandomSoundByTag(soundTag, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            PlaySound(eventName, soundPath, playMode: SoundPlayMode.SkipIfPlaying);
        }

        private void OnDeath(DamageInfo damageInfo)
        {
            var isCrit = damageInfo.crit > 0;

            if (CustomAnimatorControl != null)
            {
                if (isCrit)
                    CustomAnimatorControl.TriggerCritDead();
                else
                    CustomAnimatorControl.TriggerDead();
            }

            if (!IsModelAudioEnabled) return;

            string soundTag;
            string eventName;
            if (isCrit)
            {
                soundTag = SoundTags.TriggerOnCritDead;
                eventName = "onCritDead";
            }
            else
            {
                soundTag = SoundTags.TriggerOnDeath;
                eventName = "onDeath";
            }

            var soundPath = GetRandomSoundByTag(soundTag, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            StopAllSounds();
            PlaySound(eventName, soundPath, playMode: SoundPlayMode.UseTempObject);
        }

        private void OnGlobalHurt(Health health, DamageInfo damageInfo)
        {
            if (CharacterMainControl == null || CustomAnimatorControl == null) return;
            if (damageInfo.fromCharacter != CharacterMainControl) return;

            var isCrit = damageInfo.crit > 0;

            if (isCrit)
                CustomAnimatorControl.TriggerCritHitTarget();
            else
                CustomAnimatorControl.TriggerHitTarget();

            if (!IsModelAudioEnabled) return;

            string soundTag;
            string eventName;
            if (isCrit)
            {
                soundTag = SoundTags.TriggerOnCritHitTarget;
                eventName = "onCritHitTarget";
            }
            else
            {
                soundTag = SoundTags.TriggerOnHitTarget;
                eventName = "onHitTarget";
            }

            var soundPath = GetRandomSoundByTag(soundTag, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            PlaySound(eventName, soundPath, playMode: SoundPlayMode.SkipIfPlaying);
        }

        private void OnGlobalDead(Health health, DamageInfo damageInfo)
        {
            if (CharacterMainControl == null || CustomAnimatorControl == null) return;
            if (damageInfo.fromCharacter != CharacterMainControl) return;

            var isCrit = damageInfo.crit > 0;

            if (isCrit)
                CustomAnimatorControl.TriggerCritKillTarget();
            else
                CustomAnimatorControl.TriggerKillTarget();

            if (!IsModelAudioEnabled) return;

            string soundTag;
            string eventName;
            if (isCrit)
            {
                soundTag = SoundTags.TriggerOnCritKillTarget;
                eventName = "onCritKillTarget";
            }
            else
            {
                soundTag = SoundTags.TriggerOnKillTarget;
                eventName = "onKillTarget";
            }

            var soundPath = GetRandomSoundByTag(soundTag, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            PlaySound(eventName, soundPath, playMode: SoundPlayMode.SkipIfPlaying);
        }

        private void InitializeCustomCharacterSubVisuals()
        {
            if (CustomModelInstance == null || _customModelSubVisuals != null) return;

            var subVisuals = CustomModelInstance.GetComponent<CharacterSubVisuals>();
            if (subVisuals == null)
            {
                subVisuals = CustomModelInstance.AddComponent<CharacterSubVisuals>();
                subVisuals.renderers = [];
                subVisuals.particles = [];
                subVisuals.lights = [];
                subVisuals.sodaPointLights = [];
                subVisuals.character = CharacterMainControl;
                subVisuals.mainModel = OriginalCharacterModel;
            }

            _customModelSubVisuals = subVisuals;
            _customModelSubVisuals.SetRenderers();
        }

        private void InitializeCustomCharacterSoundMaker(ModelInfo? modelInfo = null)
        {
            if (CharacterMainControl == null || CustomModelInstance == null) return;

            var soundMaker = CustomModelInstance.GetComponent<CustomCharacterSoundMaker>();
            if (soundMaker == null)
            {
                soundMaker = CustomModelInstance.gameObject.AddComponent<CustomCharacterSoundMaker>();
                soundMaker.characterMainControl = CharacterMainControl;

                var originalSoundMaker = CharacterMainControl.GetComponent<CharacterSoundMaker>();
                if (originalSoundMaker != null)
                {
                    soundMaker.characterMainControl = originalSoundMaker.characterMainControl;
                    soundMaker.originalCharacterSoundMaker = originalSoundMaker;
                }
            }

            if (modelInfo != null)
            {
                if (modelInfo.WalkSoundFrequency.HasValue)
                    soundMaker.CustomWalkSoundFrequency = modelInfo.WalkSoundFrequency.Value;
                if (modelInfo.RunSoundFrequency.HasValue)
                    soundMaker.CustomRunSoundFrequency = modelInfo.RunSoundFrequency.Value;
            }

            _customCharacterSoundMaker = soundMaker;
        }

        private static string GetEffectiveAICharacterConfigKey(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return AICharacters.AllAICharactersKey;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return nameKey;

            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            var modelID = usingModel.GetModelID(targetTypeId);
            return !string.IsNullOrEmpty(modelID) ? nameKey : AICharacters.AllAICharactersKey;
        }

        private static Transform? SearchLocatorTransform(GameObject modelInstance, string locatorName)
        {
            var transforms = modelInstance.GetComponentsInChildren<Transform>(true);
            return transforms.FirstOrDefault(t => t.name == locatorName);
        }

        private static Renderer[] GetAllRenderers(GameObject targetGameObject)
        {
            return targetGameObject.GetComponentsInChildren<Renderer>(true);
        }

        private static void ReplaceRenderersLayer(Renderer[] renderers, string layerName = "Character")
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                ModLogger.LogError($"Layer '{layerName}' not found.");
                return;
            }

            foreach (var renderer in renderers)
            {
                var gameObject = renderer.gameObject;
                if (gameObject.layer != layer)
                    gameObject.layer = layer;
            }
        }

        private static void ReplaceRenderersShader(Renderer[] renderers, string? shaderName = null)
        {
            var shader = shaderName != null ? Shader.Find(shaderName) : GameDefaultShader;
            if (shader == null)
            {
                ModLogger.LogError(shaderName != null
                    ? $"Shader '{shaderName}' not found."
                    : "Game default shader not found.");
                return;
            }

            if (shader == GameDefaultShader) // SodaCraft/SodaCharacter shader needs to disable emission
                foreach (var renderer in renderers)
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    material.shader = shader;
                    if (material.HasProperty(EmissionColor))
                        material.SetColor(EmissionColor, Color.black);
                }
            else
                foreach (var renderer in renderers)
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    material.shader = shader;
                }
        }

        #region Shader Constants

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        private static Shader GameDefaultShader => Shader.Find("SodaCraft/SodaCharacter");

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        private static Shader GameCharacterShowBackShader => Shader.Find("CharacterShowBack");

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        #endregion

        #region Sound System

        private void InitSoundFilePath(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            _soundsByTag.Clear();
            _soundTagPlayChance.Clear();

            var bundleDirectory = modelBundleInfo.DirectoryPath;

            if (modelInfo.SoundTagPlayChance != null)
                foreach (var kvp in modelInfo.SoundTagPlayChance)
                {
                    var normalizedTag = kvp.Key.ToLowerInvariant().Trim();
                    if (string.IsNullOrWhiteSpace(normalizedTag)) continue;
                    var chance = Mathf.Clamp01(kvp.Value / 100f);
                    _soundTagPlayChance[normalizedTag] = chance;
                }

            if (modelInfo.CustomSounds is not { Length: > 0 }) return;

            var validSoundCount = 0;

            foreach (var soundInfo in modelInfo.CustomSounds)
            {
                if (string.IsNullOrWhiteSpace(soundInfo.Path)) continue;
                var fullPath = Path.Combine(bundleDirectory, soundInfo.Path);
                if (!File.Exists(fullPath)) continue;

                if (soundInfo.Tags is not { Length: > 0 })
                    soundInfo.Tags = [SoundTags.Normal];

                foreach (var soundTag in soundInfo.Tags
                             .Select(soundTag => soundTag?.ToLowerInvariant().Trim())
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Cast<string>())
                {
                    if (!_soundsByTag.ContainsKey(soundTag))
                        _soundsByTag[soundTag] = [];
                    _soundsByTag[soundTag].Add(fullPath);
                }

                validSoundCount++;
            }

            if (validSoundCount == 0) return;
            ModLogger.Log(
                $"Initialized {validSoundCount} custom sounds for model '{modelInfo.Name}' ({modelInfo.ModelID}).");
        }

        public bool HasAnySounds()
        {
            return _soundsByTag.Count > 0 && _soundsByTag.Values.Any(sounds => sounds.Count > 0);
        }

        public bool HasSoundTag(string soundTag)
        {
            if (string.IsNullOrWhiteSpace(soundTag)) soundTag = SoundTags.Normal;
            soundTag = soundTag.ToLowerInvariant().Trim();
            return _soundsByTag.TryGetValue(soundTag, out var sounds) && sounds.Count > 0;
        }

        public string? GetRandomSoundByTag(string soundTag, out bool skippedByProbability)
        {
            skippedByProbability = false;
            if (string.IsNullOrWhiteSpace(soundTag)) soundTag = SoundTags.Normal;
            soundTag = soundTag.ToLowerInvariant().Trim();

            if (!_soundsByTag.TryGetValue(soundTag, out var sounds) || sounds.Count == 0) return null;

            if (_soundTagPlayChance.TryGetValue(soundTag, out var playChance) && Random.value > playChance)
                skippedByProbability = true;

            var index = Random.Range(0, sounds.Count);
            return sounds[index];
        }

        public string? GetRandomSoundByTag(string soundTag)
        {
            return GetRandomSoundByTag(soundTag, out _);
        }

        public EventInstance? PlaySound(
            string eventName,
            string path,
            bool loop = false,
            SoundPlayMode playMode = SoundPlayMode.Normal,
            float volume = 1f)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (!_playingSoundInstances.TryGetValue(eventName, out var existingInstances))
            {
                existingInstances = [];
                _playingSoundInstances[eventName] = existingInstances;
            }

            EventInstance? eventInstance;

            switch (playMode)
            {
                case SoundPlayMode.StopPrevious:
                    StopSound(eventName);
                    goto default;
                case SoundPlayMode.SkipIfPlaying:
                    if (existingInstances.Any(AudioUtils.CheckSoundIsPlaying))
                        return null;
                    goto default;
                case SoundPlayMode.UseTempObject:
                    if (loop)
                        ModLogger.LogWarning(
                            $"Sound '{eventName}' is set to loop, but 'useTempObject' is true. Loop will be ignored.");
                    eventInstance = AudioUtils.PlayAudioWithTempObject(path, gameObject.transform);
                    break;
                default:
                    eventInstance = AudioManager.Instance.MPostCustomSFX(path, gameObject, loop);
                    break;
            }

            if (eventInstance == null)
            {
                ModLogger.LogError($"Failed to play sound '{eventName}' from path: {path}");
                return null;
            }

            var finalVolume = volume * ModelAudioVolume;
            eventInstance.Value.setVolume(finalVolume);

            existingInstances.Add(eventInstance.Value);

            return eventInstance;
        }

        public bool IsSoundPlaying(string eventName)
        {
            return _playingSoundInstances.TryGetValue(eventName, out var existingInstances) &&
                   existingInstances.Any(AudioUtils.CheckSoundIsPlaying);
        }

        public void StopSound(string eventName)
        {
            if (!_playingSoundInstances.TryGetValue(eventName, out var existingInstances)) return;

            foreach (var existingInstance in existingInstances)
            {
                existingInstance.stop(STOP_MODE.IMMEDIATE);
                existingInstance.release();
            }

            existingInstances.Clear();
        }

        public void StopAllSounds()
        {
            foreach (var existingInstances in _playingSoundInstances.Select(kvp => kvp.Value))
            {
                foreach (var existingInstance in existingInstances)
                {
                    existingInstance.stop(STOP_MODE.IMMEDIATE);
                    existingInstance.release();
                }

                existingInstances.Clear();
            }

            _playingSoundInstances.Clear();
        }

        private void RefreshPlayingSounds()
        {
            var keys = _playingSoundInstances.Keys.ToArray();
            foreach (var eventName in keys)
            {
                var existingInstances = _playingSoundInstances[eventName];
                existingInstances.RemoveAll(existingInstance => !AudioUtils.CheckSoundIsPlaying(existingInstance));
                if (existingInstances.Count == 0)
                    _playingSoundInstances.Remove(eventName);
            }
        }

        private bool HasIdleSounds()
        {
            return _soundsByTag.ContainsKey(SoundTags.Idle) &&
                   _soundsByTag[SoundTags.Idle].Count > 0;
        }

        private void PlayIdleAudio()
        {
            if (!IsModelAudioEnabled) return;

            var soundPath = GetRandomSoundByTag(SoundTags.Idle, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            PlaySound("idle", soundPath);
        }

        private void OnSoundTriggered(string soundTag, string eventName, SoundPlayMode playMode, Animator animator)
        {
            if (animator == null) return;
            if (CustomModelInstance == null) return;
            if (animator.gameObject != CustomModelInstance) return;

            if (!IsModelAudioEnabled) return;

            var soundPath = GetRandomSoundByTag(soundTag, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            PlaySound(eventName, soundPath, playMode: playMode);
        }

        private void OnSoundStopTriggered(string eventName, Animator animator)
        {
            if (animator == null) return;
            if (CustomModelInstance == null) return;
            if (animator.gameObject != CustomModelInstance) return;

            if (string.IsNullOrEmpty(eventName))
                StopAllSounds();
            else
                StopSound(eventName);
        }

        private void ScheduleNextIdleAudio()
        {
            if (ModEntry.IdleAudioConfig == null)
            {
                _nextIdleAudioTime = Time.time + Random.Range(30f, 45f);
                return;
            }

            var interval = ModEntry.IdleAudioConfig.GetIdleAudioInterval(TargetTypeId);

            var randomInterval = Random.Range(interval.Min, interval.Max);
            _nextIdleAudioTime = Time.time + randomInterval;
        }


        public void NotifyModelChanged(bool isRestored)
        {
            ModelListManager.NotifyModelChanged(this, isRestored);
        }

        #endregion

        #region 过时成员（向后兼容）

        [Obsolete("Use Initialize(CharacterMainControl characterMainControl, string targetTypeId) instead.")]
        public void Initialize(CharacterMainControl characterMainControl, ModelTarget target = ModelTarget.Character)
        {
            var targetTypeId = target.ToTargetTypeId();
            Initialize(characterMainControl, targetTypeId);
        }

        [Obsolete("Use TargetTypeId instead. This property is kept for backward compatibility.")]
        public ModelTarget Target
        {
            get
            {
                var target = ModelTargetExtensions.FromTargetTypeId(TargetTypeId);
                return target ?? ModelTarget.Character;
            }
            private set => TargetTypeId = value.ToTargetTypeId();
        }

        [Obsolete(
            "TargetTypeId is read-only and can only be set during initialization. Re-call Initialize(CharacterMainControl, string targetTypeId) if you need to change the target type. This method is kept for backward compatibility.")]
        public void SetTarget(ModelTarget target)
        {
            Target = target;
        }

        #endregion
    }
}
