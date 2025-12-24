using System.Collections.Generic;
using System.Linq;
using Duckov.Buffs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
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

        private bool _attacking;
        private int _attackLayerIndex = -1;
        private float _attackTimer;
        private float _attackWeight;
        private CharacterMainControl? _characterMainControl;
        private CharacterModel? _characterModel;
        private Animator? _customAnimator;
        private ItemAgent_Gun? _gunAgent;
        private DuckovItemAgent? _holdAgent;
        private bool _initialized;
        private ModelHandler? _modelHandler;

        private bool HasAnimationIfDashCanControl
        {
            get
            {
                if (_modelHandler == null)
                    return false;
                return _modelHandler.OriginalAnimationControl != null &&
                       _modelHandler.OriginalAnimationControl.hasAnimationIfDashCanControl;
            }
        }

        private AnimationCurve? AttackLayerWeightCurve
        {
            get
            {
                if (_modelHandler == null)
                    return null;
                if (_modelHandler.OriginalAnimationControl != null)
                    return _modelHandler.OriginalAnimationControl.attackLayerWeightCurve;
                return _modelHandler.OriginalMagicBlendAnimationControl != null
                    ? _modelHandler.OriginalMagicBlendAnimationControl.attackLayerWeightCurve
                    : null;
            }
        }

        private float AttackTime
        {
            get
            {
                if (_modelHandler == null)
                    return 0.3f;
                if (_modelHandler.OriginalAnimationControl != null)
                    return _modelHandler.OriginalAnimationControl.attackTime;
                return _modelHandler.OriginalMagicBlendAnimationControl != null
                    ? _modelHandler.OriginalMagicBlendAnimationControl.attackTime
                    : 0.3f;
            }
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            var context = new AnimatorUpdateContext
            {
                Initialized = _initialized,
                CharacterMainControl = _characterMainControl,
                CharacterModel = _characterModel,
                ModelHandler = _modelHandler,
                HoldAgent = _holdAgent,
                GunAgent = _gunAgent,
                Attacking = _attacking,
                AttackTimer = _attackTimer,
                AttackWeight = _attackWeight,
                HasAnimationIfDashCanControl = HasAnimationIfDashCanControl,
                AttackLayerWeightCurve = AttackLayerWeightCurve,
                AttackTime = AttackTime,
            };

            AnimatorParameterUpdaterManager.UpdateAll(this, context);

            _attacking = context.Attacking;
            _attackTimer = context.AttackTimer;
            _attackWeight = context.AttackWeight;
            _holdAgent = context.HoldAgent;
            _gunAgent = context.GunAgent;

            UpdateBuffParams();
        }

        private void OnDestroy()
        {
            if (_characterModel != null) _characterModel.OnAttackOrShootEvent -= OnAttack;
            if (_characterMainControl != null) _characterMainControl.OnHoldAgentChanged -= OnHoldAgentChanged;
            UnsubscribeGunEvents();
        }

        public void Initialize(ModelHandler modelHandler)
        {
            if (modelHandler == null) return;

            switch (_initialized)
            {
                case true when _modelHandler == modelHandler:
                    return;
                case true:
                {
                    if (_characterModel != null) _characterModel.OnAttackOrShootEvent -= OnAttack;
                    if (_characterMainControl != null) _characterMainControl.OnHoldAgentChanged -= OnHoldAgentChanged;
                    UnsubscribeGunEvents();
                    break;
                }
            }

            _modelHandler = modelHandler;
            _characterMainControl = modelHandler.CharacterMainControl;
            _characterModel = modelHandler.OriginalCharacterModel;
            if (_characterMainControl == null || _characterModel == null)
                return;

            _characterModel.OnAttackOrShootEvent += OnAttack;
            _characterMainControl.OnHoldAgentChanged += OnHoldAgentChanged;

            _customAnimator = modelHandler.CustomAnimator;
            FindMeleeAttackLayerIndex();

            _initialized = true;

            InitializeAnimatorParamCache();
            InitializeBuffParamCache();
            RegisterCoreUpdaters();

            OnHoldAgentChanged(_characterMainControl.CurrentHoldItemAgent);
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
            _attacking = true;
            _attackTimer = 0.0f;
            FindMeleeAttackLayerIndex();
            SetAnimatorTrigger(CustomAnimatorHash.Attack);
        }

        private void OnHoldAgentChanged(DuckovItemAgent? agent)
        {
            UnsubscribeGunEvents();

            _holdAgent = agent;
            _gunAgent = agent as ItemAgent_Gun;

            if (_gunAgent == null) return;
            _gunAgent.OnShootEvent += OnShoot;
            _gunAgent.OnLoadedEvent += OnLoaded;
        }

        private void UnsubscribeGunEvents()
        {
            if (_gunAgent == null) return;
            _gunAgent.OnShootEvent -= OnShoot;
            _gunAgent.OnLoadedEvent -= OnLoaded;
        }

        private void OnShoot()
        {
            SetAnimatorTrigger(CustomAnimatorHash.Shoot);
            if (_gunAgent != null && _gunAgent.BulletCount > 0)
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

            if (_modelHandler == null) return;
            if (_modelHandler.CurrentModelInfo?.BuffAnimatorParams == null) return;

            foreach (var (paramName, conditions) in _modelHandler.CurrentModelInfo.BuffAnimatorParams)
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
            if (!_initialized) return;
            if (_modelHandler == null || _customAnimator == null) return;
            if (_buffParamConditions.Count == 0) return;

            var buffs = _modelHandler.Buffs;
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
