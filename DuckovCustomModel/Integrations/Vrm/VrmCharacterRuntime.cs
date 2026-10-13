using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;
using UniVRM10;

namespace DuckovCustomModel.Integrations.Vrm
{
    [DefaultExecutionOrder(10900)]
    internal sealed class VrmCharacterRuntime : MonoBehaviour
    {
        private readonly List<(Transform socket, Transform original)> _socketRotations = [];
        private Animator? _animator;
        private ModelHandler? _handler;
        private int _holdingLayer, _attackLayer;
        private Vrm10Instance? _instance;
        private float _invisibleSince;
        private Vector3 _previousPosition;
        private Renderer[] _renderers = [];
        private float _rollPhase;
        private VrmModelSettings _settings = new();
        private bool _springSuspended, _initialized, _registered;
        internal bool OriginalAutoSyncRightHandRotation { get; private set; }
        internal float NaturalHeight { get; private set; }
        internal Vector3 InitialRootScale { get; private set; }

        private void Update()
        {
            if (_animator == null || _handler?.CharacterMainControl == null ||
                !string.IsNullOrWhiteSpace(_settings.AnimatorControllerPath)) return;
            var character = _handler.CharacterMainControl;
            var movement = character.movementControl;
            var velocity = character.Velocity;
            var speed = new Vector2(velocity.x, velocity.z).magnitude;
            var moveSpeed = character.AnimationMoveSpeedValue;
            if (movement != null && movement.walkSpeed > .0001f)
                moveSpeed = movement.Running && speed > movement.walkSpeed
                    ? 1 + Mathf.InverseLerp(movement.walkSpeed, movement.runSpeed, speed)
                    : Mathf.Clamp01(speed / movement.walkSpeed);
            _animator.SetFloat(CustomAnimatorHash.MoveSpeed,
                float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed) ? 0 : Mathf.Clamp(moveSpeed, 0, 2));
            var dash = _handler.CharacterMainControl.CurrentAction as CA_Dash;
            var rolling = dash is { Running: true };
            if (rolling)
            {
                var progress = dash!.GetProgress();
                _rollPhase = progress.total > 0 ? Mathf.Clamp01(progress.current / progress.total) : 0;
            }

            _animator.SetBool("RollPose", rolling);
            _animator.SetFloat("RollProgress", _rollPhase);
            _animator.SetBool("Dashing", rolling);
            var control = _handler.CustomAnimatorControl;
            _animator.SetFloat("AttackProgress", control is { Attacking: true, AttackTime: > 0 }
                ? Mathf.Clamp01(control.AttackTimer / control.AttackTime)
                : 0);
            if (_holdingLayer >= 0)
            {
                var holding = !rolling && (control?.GunAgent != null || _animator.GetInteger("HandState") < 0 ||
                                           _animator.GetBool("Reloading") || _animator.GetBool("UsingItem"));
                var weight = rolling
                    ? 0
                    : Mathf.MoveTowards(_animator.GetLayerWeight(_holdingLayer),
                        holding ? 1 : 0, 12 * Time.deltaTime);
                _animator.SetLayerWeight(_holdingLayer, weight);
            }

            if (_attackLayer >= 0 && (rolling || (control?.GunAgent != null &&
                                                  character.CurrentAction is not CA_Attack { Running: true })))
                _animator.SetLayerWeight(_attackLayer, 0);
        }

        private void LateUpdate()
        {
            if (_instance == null || _handler?.CharacterMainControl == null) return;
            var visible = false;
            foreach (var renderer in _renderers)
                if (renderer != null && renderer.isVisible)
                {
                    visible = true;
                    break;
                }

            if (visible)
            {
                _invisibleSince = Time.unscaledTime;
            }
            else if (Time.unscaledTime - _invisibleSince > .5f)
            {
                SuspendSpring();
                _previousPosition = transform.position;
                return;
            }

            ResumeSpring();
            if ((transform.position - _previousPosition).sqrMagnitude > 64)
                _instance.Runtime.SpringBone.RestoreInitialTransform();
            _previousPosition = transform.position;
            if ((_handler.CustomAnimatorControl is not { Attacking: true } ||
                 _handler.CustomAnimatorControl.GunAgent != null) &&
                _handler.CharacterMainControl.CurrentAction is not CA_Attack { Running: true } &&
                _handler.CharacterMainControl.CurrentAction is not CA_Dash { Running: true })
                foreach (var (socket, original) in _socketRotations)
                    if (socket != null && original != null)
                        socket.rotation = Quaternion.Slerp(socket.rotation, original.rotation,
                            1 - Mathf.Exp(-25 * Time.deltaTime));
            _instance.Runtime.SpringBone.SetModelLevel(transform, new(supportsScalingAtRuntime: true));
            _instance.Runtime.Process();
        }

        private void OnEnable()
        {
            if (!_initialized || _instance == null) return;
            VrmRenderPipeline.Register();
            _registered = true;
            _previousPosition = transform.position;
            _invisibleSince = Time.unscaledTime;
            if (_handler?.OriginalCharacterModel != null)
            {
                OriginalAutoSyncRightHandRotation = _handler.OriginalCharacterModel.autoSyncRightHandRotation;
                _handler.OriginalCharacterModel.autoSyncRightHandRotation = false;
            }

            ResumeSpring();
        }

        private void OnDisable()
        {
            if (_registered) VrmRenderPipeline.Unregister();
            _registered = false;
            if (!_initialized || _instance == null) return;
            SuspendSpring();
            if (_handler?.OriginalCharacterModel != null)
                _handler.OriginalCharacterModel.autoSyncRightHandRotation = OriginalAutoSyncRightHandRotation;
        }

        internal void Initialize(ModelHandler handler, Vrm10Instance instance, VrmModelSettings settings)
        {
            _handler = handler;
            _instance = instance;
            _settings = settings;
            InitialRootScale = transform.localScale;
            instance.UpdateType = Vrm10Instance.UpdateTypes.None;
            _renderers = GetComponentsInChildren<Renderer>(true);
            var animator = GetComponent<Animator>();
            _animator = animator;
            _holdingLayer = animator.GetLayerIndex("Holding");
            _attackLayer = animator.GetLayerIndex("MeleeAttack");
            var defaults = new Dictionary<string, HumanBodyBones>
            {
                [SocketNames.LeftHand] = HumanBodyBones.LeftHand,
                [SocketNames.RightHand] = HumanBodyBones.RightHand,
                [SocketNames.Armor] = HumanBodyBones.Chest,
                [SocketNames.Helmet] = HumanBodyBones.Head,
                [SocketNames.Face] = HumanBodyBones.Head,
                [SocketNames.Backpack] = HumanBodyBones.Chest,
                [SocketNames.MeleeWeapon] = HumanBodyBones.Hips,
                [SocketNames.PopText] = HumanBodyBones.Head,
                [SocketNames.Vehicle] = HumanBodyBones.Hips,
                [SocketNames.PaperBox] = HumanBodyBones.Hips,
                [SocketNames.Carriable] = HumanBodyBones.Chest,
            };
            foreach (var name in settings.Locators.Keys)
                if (!defaults.ContainsKey(name))
                    defaults.Add(name, HumanBodyBones.Hips);
            foreach (var pair in defaults)
            {
                settings.Locators.TryGetValue(pair.Key, out var configured);
                var bone = pair.Value;
                if (configured != null &&
                    (!Enum.TryParse(configured.Bone, true, out bone) ||
                     !Enum.IsDefined(typeof(HumanBodyBones), bone) || bone == HumanBodyBones.LastBone))
                    throw new ArgumentException("Invalid VRM humanoid bone: " + configured.Bone);
                var parent = animator.GetBoneTransform(bone);
                if (parent == null && bone == HumanBodyBones.Chest)
                    parent = animator.GetBoneTransform(HumanBodyBones.Spine);
                if (parent == null) throw new InvalidOperationException("Missing VRM socket bone: " + bone);
                var socket = new GameObject(pair.Key).transform;
                socket.SetParent(parent, false);
                if (configured != null)
                {
                    socket.localPosition = new(configured.Position[0], configured.Position[1], configured.Position[2]);
                    socket.localRotation = Quaternion.Euler(configured.Rotation[0], configured.Rotation[1],
                        configured.Rotation[2]);
                }
                else if (handler.GetOriginalSocketTransform(pair.Key) is { } original)
                {
                    socket.rotation = original.rotation;
                    if (pair.Key is SocketNames.LeftHand or SocketNames.RightHand)
                        _socketRotations.Add((socket, original));
                }

                if (handler.GetOriginalSocketTransform(pair.Key) is { } originalSocket)
                {
                    var sourceScale = originalSocket.lossyScale;
                    var parentScale = parent.lossyScale;
                    socket.localScale = new(
                        Mathf.Abs(parentScale.x) > .000001f ? sourceScale.x / parentScale.x : 1,
                        Mathf.Abs(parentScale.y) > .000001f ? sourceScale.y / parentScale.y : 1,
                        Mathf.Abs(parentScale.z) > .000001f ? sourceScale.z / parentScale.z : 1);
                }

                if (pair.Key == SocketNames.Helmet) NaturalHeight = socket.position.y - transform.position.y;
            }

            _initialized = true;
        }

        private void SuspendSpring()
        {
            if (_springSuspended || _instance == null) return;
            _instance.Runtime.SpringBone.Dispose();
            _springSuspended = true;
        }

        private void ResumeSpring()
        {
            if (!_springSuspended || _instance == null) return;
            _instance.Runtime.SpringBone.ReconstructSpringBone();
            _instance.Runtime.SpringBone.RestoreInitialTransform();
            _springSuspended = false;
        }
    }
}
