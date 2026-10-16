using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Ik;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;
using UniVRM10;

namespace DuckovCustomModel.Integrations.Vrm
{
    [DefaultExecutionOrder(10900)]
    internal sealed class VrmCharacterRuntime : MonoBehaviour
    {
        private static readonly Plane[] CameraPlanes = new Plane[6];
        private static Camera? _visibilityCamera;
        private static int _visibilityFrame = -1;
        private readonly List<(Transform socket, Transform original)> _socketRotations = [];
        private Animator? _animator;
        private ModelHandler? _handler;
        private int _holdingLayer = -1, _attackLayer = -1;
        private ModelIkController? _ik;
        private Vrm10Instance? _instance;
        private float _invisibleSince;
        private Vector3 _previousPosition;
        private Renderer[] _renderers = [];
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
            var rolling = character.CurrentAction is CA_Dash { Running: true };
            var heldGun = character.CurrentHoldItemAgent as ItemAgent_Gun;
            var holdingGun = heldGun != null && heldGun.isActiveAndEnabled;
            if (_holdingLayer >= 0)
            {
                var holding = !rolling && (holdingGun || character.carryAction.Running ||
                                           character.CurrentAction is CA_UseItem { Running: true });
                var weight = rolling
                    ? 0
                    : Mathf.MoveTowards(_animator.GetLayerWeight(_holdingLayer),
                        holding ? 1 : 0, 12 * Time.deltaTime);
                _animator.SetLayerWeight(_holdingLayer, weight);
            }

            if (_attackLayer >= 0 && (rolling || (holdingGun &&
                                                  character.CurrentAction is not CA_Attack { Running: true })))
                _animator.SetLayerWeight(_attackLayer, 0);
        }

        private void LateUpdate()
        {
            if (_instance == null || _handler?.CharacterMainControl == null) return;
            if (IsVisible())
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
            if (!_handler.IsExternalMotionPlaying && _ik is not { IsControllingHands: true } &&
                (_handler.CustomAnimatorControl is not { Attacking: true } ||
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

        private bool IsVisible()
        {
            if (_handler?.CharacterMainControl == CharacterMainControl.Main) return true;
            if (_visibilityFrame != Time.frameCount)
            {
                _visibilityFrame = Time.frameCount;
                _visibilityCamera = Camera.main;
                if (_visibilityCamera != null)
                    GeometryUtility.CalculateFrustumPlanes(_visibilityCamera, CameraPlanes);
            }

            foreach (var renderer in _renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer.isVisible || _visibilityCamera != null && _visibilityCamera.isActiveAndEnabled &&
                    (_visibilityCamera.cullingMask & (1 << renderer.gameObject.layer)) != 0 &&
                    GeometryUtility.TestPlanesAABB(CameraPlanes, renderer.bounds)) return true;
            }

            return false;
        }

        private void OnEnable()
        {
            if (!_initialized || _instance == null) return;
            _ik = GetComponent<ModelIkController>();
            if (_animator != null)
            {
                _holdingLayer = _animator.GetLayerIndex("Holding");
                _attackLayer = _animator.GetLayerIndex("MeleeAttack");
                if (string.IsNullOrWhiteSpace(_settings.AnimatorControllerPath))
                {
                    if (_holdingLayer >= 0) _animator.SetLayerWeight(_holdingLayer, 0);
                    if (_attackLayer >= 0) _animator.SetLayerWeight(_attackLayer, 0);
                }
            }

            VrmRenderPipeline.Register();
            VrmTransparentDepthFeature.Register(_renderers);
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
            if (_registered)
            {
                VrmTransparentDepthFeature.Unregister(_renderers);
                VrmRenderPipeline.Unregister();
            }
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
            var handRotations = GetHandRotations(animator);
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
                else if (handRotations.TryGetValue(bone, out var handRotation) &&
                         pair.Key is SocketNames.LeftHand or SocketNames.RightHand)
                {
                    socket.localRotation = handRotation;
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

        private Dictionary<HumanBodyBones, Quaternion> GetHandRotations(Animator animator)
        {
            var rotations = new Dictionary<HumanBodyBones, Quaternion>();
            if (HumanoidHandFrame.TryGetLocalRotation(animator, false, out var right))
                rotations[HumanBodyBones.RightHand] = right;
            if (HumanoidHandFrame.TryGetLocalRotation(animator, true, out var left))
                rotations[HumanBodyBones.LeftHand] = left;
            return rotations;
        }

        private void SuspendSpring()
        {
            if (_springSuspended || _instance == null) return;
            _springSuspended = true;
        }

        private void ResumeSpring()
        {
            if (!_springSuspended || _instance == null) return;
            _instance.Runtime.SpringBone.RestoreInitialTransform();
            _springSuspended = false;
        }
    }
}
