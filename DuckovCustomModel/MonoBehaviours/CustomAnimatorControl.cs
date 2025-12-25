using System.Collections.Generic;
using System.Linq;
using Duckov.Buffs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Managers.Updaters;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomAnimatorControl : MonoBehaviour
    {
        private readonly Dictionary<int, bool> _boolParams = new();

        private readonly Dictionary<int, BuffCondition[]> _buffParamConditions = [];

        private readonly Dictionary<int, float> _floatParams = new();
        private readonly Dictionary<int, int> _intParams = new();
        private readonly HashSet<int> _validBoolParamHashes = [];
        private readonly HashSet<int> _validFloatParamHashes = [];
        private readonly HashSet<int> _validIntParamHashes = [];

        private int _attackLayerIndex = -1;
        private Animator? _customAnimator;

        public bool Initialized { get; private set; }

        public CharacterMainControl? CharacterMainControl { get; private set; }

        public CharacterModel? CharacterModel { get; private set; }

        public ModelHandler? ModelHandler { get; private set; }

        public DuckovItemAgent? HoldAgent { get; private set; }

        public ItemAgent_Gun? GunAgent { get; private set; }

        public bool Attacking { get; set; }

        public float AttackTimer { get; set; }

        public float AttackWeight { get; set; }

        public bool HasAnimationIfDashCanControl => ModelHandler != null &&
                                                    ModelHandler.OriginalAnimationControl != null &&
                                                    ModelHandler.OriginalAnimationControl.hasAnimationIfDashCanControl;

        public AnimationCurve? AttackLayerWeightCurve
        {
            get
            {
                if (ModelHandler == null) return null;
                if (ModelHandler.OriginalAnimationControl != null)
                    return ModelHandler.OriginalAnimationControl.attackLayerWeightCurve;
                return ModelHandler.OriginalMagicBlendAnimationControl != null
                    ? ModelHandler.OriginalMagicBlendAnimationControl.attackLayerWeightCurve
                    : null;
            }
        }

        public float AttackTime
        {
            get
            {
                if (ModelHandler == null) return 0.3f;
                if (ModelHandler.OriginalAnimationControl != null)
                    return ModelHandler.OriginalAnimationControl.attackTime;
                return ModelHandler.OriginalMagicBlendAnimationControl != null
                    ? ModelHandler.OriginalMagicBlendAnimationControl.attackTime
                    : 0.3f;
            }
        }

        private void Update()
        {
            if (!Initialized) return;
            if (CharacterMainControl == null)
                return;

            AnimatorParameterUpdaterManager.UpdateAll(this);

            UpdateBuffParams();
        }

        private void OnDestroy()
        {
            if (CharacterModel != null) CharacterModel.OnAttackOrShootEvent -= OnAttack;
            if (CharacterMainControl != null) CharacterMainControl.OnHoldAgentChanged -= OnHoldAgentChanged;
            UnsubscribeGunEvents();
        }

        public void Initialize(ModelHandler modelHandler)
        {
            if (modelHandler == null) return;

            switch (Initialized)
            {
                case true when ModelHandler == modelHandler:
                    return;
                case true:
                {
                    if (CharacterModel != null) CharacterModel.OnAttackOrShootEvent -= OnAttack;
                    if (CharacterMainControl != null) CharacterMainControl.OnHoldAgentChanged -= OnHoldAgentChanged;
                    UnsubscribeGunEvents();
                    break;
                }
            }

            ModelHandler = modelHandler;
            CharacterMainControl = modelHandler.CharacterMainControl;
            CharacterModel = modelHandler.OriginalCharacterModel;
            if (CharacterMainControl == null || CharacterModel == null)
                return;

            CharacterModel.OnAttackOrShootEvent += OnAttack;
            CharacterMainControl.OnHoldAgentChanged += OnHoldAgentChanged;

            _customAnimator = modelHandler.CustomAnimator;
            FindMeleeAttackLayerIndex();

            Initialized = true;

            InitializeAnimatorParamCache();
            InitializeBuffParamCache();
            RegisterCoreUpdaters();

            OnHoldAgentChanged(CharacterMainControl.CurrentHoldItemAgent);
        }

        private static void RegisterCoreUpdaters()
        {
            AnimatorParameterUpdaterManager.Register(new DeadStateUpdater());
            AnimatorParameterUpdaterManager.Register(new MovementUpdater());
            AnimatorParameterUpdaterManager.Register(new VelocityAndAimUpdater());
            AnimatorParameterUpdaterManager.Register(new CharacterTypeUpdater());
            AnimatorParameterUpdaterManager.Register(new CharacterStatusUpdater());
            AnimatorParameterUpdaterManager.Register(new EquipmentStateUpdater());
            AnimatorParameterUpdaterManager.Register(new EquipmentTypeIDUpdater());
            AnimatorParameterUpdaterManager.Register(new HandStateUpdater());
            AnimatorParameterUpdaterManager.Register(new HandTypeIDUpdater());
            AnimatorParameterUpdaterManager.Register(new GunStateUpdater());
            AnimatorParameterUpdaterManager.Register(new AttackLayerWeightUpdater());
            AnimatorParameterUpdaterManager.Register(new ActionStateUpdater());
            AnimatorParameterUpdaterManager.Register(new TimeAndWeatherUpdater());
        }

        public void SetCustomAnimator(Animator? animator)
        {
            _customAnimator = animator;
            _attackLayerIndex = -1;
            if (_customAnimator != null) FindMeleeAttackLayerIndex();
            InitializeAnimatorParamCache();
            InitializeBuffParamCache();
        }

        private void InitializeAnimatorParamCache()
        {
            _validBoolParamHashes.Clear();
            _validIntParamHashes.Clear();
            _validFloatParamHashes.Clear();

            if (_customAnimator == null) return;

            foreach (var param in _customAnimator.parameters)
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        _validBoolParamHashes.Add(param.nameHash);
                        break;
                    case AnimatorControllerParameterType.Int:
                        _validIntParamHashes.Add(param.nameHash);
                        break;
                    case AnimatorControllerParameterType.Float:
                        _validFloatParamHashes.Add(param.nameHash);
                        break;
                }
        }

        private void SetAnimatorFloat(int hash, float value)
        {
            _floatParams[hash] = value;
            if (_customAnimator != null && _validFloatParamHashes.Contains(hash))
                _customAnimator.SetFloat(hash, value);
        }

        private void SetAnimatorInteger(int hash, int value)
        {
            _intParams[hash] = value;
            if (_customAnimator != null && _validIntParamHashes.Contains(hash))
                _customAnimator.SetInteger(hash, value);
        }

        private void SetAnimatorBool(int hash, bool value)
        {
            _boolParams[hash] = value;
            if (_customAnimator != null && _validBoolParamHashes.Contains(hash))
                _customAnimator.SetBool(hash, value);
        }

        private void SetAnimatorTrigger(int hash)
        {
            if (_customAnimator != null)
                _customAnimator.SetTrigger(hash);
        }

        public float GetParameterFloat(int hash)
        {
            return _floatParams.GetValueOrDefault(hash, 0f);
        }

        public int GetParameterInteger(int hash)
        {
            return _intParams.GetValueOrDefault(hash, 0);
        }

        public bool GetParameterBool(int hash)
        {
            return _boolParams.TryGetValue(hash, out var value) && value;
        }

        public void SetParameterFloat(int hash, float value)
        {
            SetAnimatorFloat(hash, value);
        }

        public void SetParameterInteger(int hash, int value)
        {
            SetAnimatorInteger(hash, value);
        }

        public void SetParameterBool(int hash, bool value)
        {
            SetAnimatorBool(hash, value);
        }

        public void SetMeleeAttackLayerWeight(float weight)
        {
            if (_attackLayerIndex < 0 || _customAnimator == null)
                return;
            _customAnimator.SetLayerWeight(_attackLayerIndex, weight);
        }


        private void FindMeleeAttackLayerIndex()
        {
            if (_customAnimator == null) return;
            if (_attackLayerIndex < 0)
                _attackLayerIndex = _customAnimator.GetLayerIndex("MeleeAttack");
            if (_attackLayerIndex >= 0)
                _customAnimator.SetLayerWeight(_attackLayerIndex, 0);
        }

        private void OnAttack()
        {
            Attacking = true;
            AttackTimer = 0.0f;
            FindMeleeAttackLayerIndex();
            SetAnimatorTrigger(CustomAnimatorHash.Attack);
        }

        private void OnHoldAgentChanged(DuckovItemAgent? agent)
        {
            UnsubscribeGunEvents();

            HoldAgent = agent;
            GunAgent = agent as ItemAgent_Gun;

            if (GunAgent == null) return;
            GunAgent.OnShootEvent += OnShoot;
            GunAgent.OnLoadedEvent += OnLoaded;
        }

        public void SetHoldAgent(DuckovItemAgent? agent)
        {
            HoldAgent = agent;
            GunAgent = agent as ItemAgent_Gun;
        }

        private void UnsubscribeGunEvents()
        {
            if (GunAgent == null) return;
            GunAgent.OnShootEvent -= OnShoot;
            GunAgent.OnLoadedEvent -= OnLoaded;
        }

        private void OnShoot()
        {
            SetAnimatorTrigger(CustomAnimatorHash.Shoot);
            if (GunAgent != null && GunAgent.BulletCount > 0)
                return;
            SetAnimatorBool(CustomAnimatorHash.Loaded, false);
        }

        private void OnLoaded()
        {
            SetAnimatorBool(CustomAnimatorHash.Loaded, true);
        }

        public void TriggerHurt()
        {
            SetAnimatorTrigger(CustomAnimatorHash.Hurt);
        }

        public void TriggerDead()
        {
            SetAnimatorTrigger(CustomAnimatorHash.Dead);
        }

        public void TriggerHitTarget()
        {
            SetAnimatorTrigger(CustomAnimatorHash.HitTarget);
        }

        public void TriggerKillTarget()
        {
            SetAnimatorTrigger(CustomAnimatorHash.KillTarget);
        }

        public void TriggerCritHurt()
        {
            SetAnimatorTrigger(CustomAnimatorHash.CritHurt);
        }

        public void TriggerCritDead()
        {
            SetAnimatorTrigger(CustomAnimatorHash.CritDead);
        }

        public void TriggerCritHitTarget()
        {
            SetAnimatorTrigger(CustomAnimatorHash.CritHitTarget);
        }

        public void TriggerCritKillTarget()
        {
            SetAnimatorTrigger(CustomAnimatorHash.CritKillTarget);
        }

        private void InitializeBuffParamCache()
        {
            _buffParamConditions.Clear();

            if (ModelHandler == null) return;
            if (ModelHandler.CurrentModelInfo?.BuffAnimatorParams == null) return;

            foreach (var (paramName, conditions) in ModelHandler.CurrentModelInfo.BuffAnimatorParams)
            {
                if (string.IsNullOrWhiteSpace(paramName) || conditions == null || conditions.Length == 0) continue;

                var paramHash = Animator.StringToHash(paramName);
                var validConditions = conditions
                    .Where(condition =>
                        condition != null &&
                        (condition.Id.HasValue || !string.IsNullOrWhiteSpace(condition.DisplayNameKey)))
                    .ToArray();

                if (validConditions.Length > 0)
                    _buffParamConditions[paramHash] = validConditions;
            }
        }

        private void UpdateBuffParams()
        {
            if (!Initialized) return;
            if (ModelHandler == null || _customAnimator == null) return;
            if (_buffParamConditions.Count == 0) return;

            var buffs = ModelHandler.Buffs;
            if (buffs is not { Count : > 0 })
            {
                foreach (var paramHash in _buffParamConditions.Keys)
                    SetAnimatorBool(paramHash, false);
                return;
            }

            foreach (var (paramHash, conditions) in _buffParamConditions)
            {
                var hasMatchingBuff = false;
                foreach (var condition in conditions)
                {
                    var matched = false;
                    if (condition.Id.HasValue) matched = buffs.Any(buff => GetBuffId(buff) == condition.Id.Value);

                    if (!matched && !string.IsNullOrWhiteSpace(condition.DisplayNameKey))
                        matched = buffs.Any(buff => GetBuffDisplayNameKey(buff) == condition.DisplayNameKey);

                    if (!matched) continue;
                    hasMatchingBuff = true;
                    break;
                }

                SetAnimatorBool(paramHash, hasMatchingBuff);
            }
        }

        private static int GetBuffId(Buff buff)
        {
            return buff.ID;
        }

        private static string? GetBuffDisplayNameKey(Buff buff)
        {
            return buff.DisplayNameKey;
        }
    }
}
