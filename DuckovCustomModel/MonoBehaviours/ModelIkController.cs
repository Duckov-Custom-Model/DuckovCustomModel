using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Ik;
using DuckovCustomModel.Integrations.Vrm;
using ModelRuntime.Adapters;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    [DefaultExecutionOrder(10800)]
    public sealed class ModelIkController : MonoBehaviour
    {
        private ModelAdapter? _adapter;
        private Quaternion _baselineRootRotation;
        private ItemAgent_Gun? _gun;
        private Vector3 _handToSocketOffset;
        private Quaternion _handToSocketRotation = Quaternion.identity;
        private ModelHandler? _handler;
        private bool _restoreRight, _restoreLeft, _externalUpdate, _ownsAutoSync, _calibrated;
        private IModelIkRig? _rig;
        private ArmIkPose _rightBaseline, _leftBaseline;
        private Transform? _rightSocket, _leftSocket;
        private ModelIkSettings? _settings;
        private Quaternion _socketToMuzzleRotation = Quaternion.identity;
        private float _weight;

        public bool IsControllingHands { get; private set; }
        public bool OriginalAutoSyncRightHandRotation { get; private set; }

        private void Update()
        {
            if (!_externalUpdate) RestoreBaseline();
        }

        private void LateUpdate()
        {
            if (!_externalUpdate) ApplyPose();
        }

        private void OnEnable()
        {
            TakeAutoSync();
        }

        private void OnDisable()
        {
            RestoreBaseline();
            RestoreAutoSync();
            IsControllingHands = false;
            _weight = 0;
            _calibrated = false;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RestoreAutoSync();
        }

        public void Initialize(ModelHandler handler, ModelIkSettings settings, IModelIkRig rig,
            bool externalUpdate = false)
        {
            Unsubscribe();
            RestoreBaseline();
            RestoreAutoSync();
            _handler = handler;
            _settings = settings;
            _rig = rig;
            _externalUpdate = externalUpdate;
            _weight = 0;
            _gun = null;
            _calibrated = false;
            _rightSocket = handler.GetSocketTransform(SocketNames.RightHand);
            CalibrateHand();
            if (externalUpdate && handler.YsmRuntime != null)
            {
                _adapter = handler.YsmRuntime.Adapter;
                _adapter.PoseEvaluated += ApplyPose;
            }

            TakeAutoSync();
        }

        public void ApplyPose()
        {
            IsControllingHands = false;
            if (!isActiveAndEnabled || _handler == null || _settings == null || _rig == null || !_rig.Root)
                return;
            if (_externalUpdate) _handler.YsmRuntime?.UpdateIkSockets();
            var character = _handler.CharacterMainControl;
            var control = _handler.CustomAnimatorControl;
            if (character == null || control == null || character.Health is { IsDead: true } ||
                character.CurrentAction is CA_Dash { Running: true })
            {
                _weight = 0;
                RestoreBaseline();
                return;
            }

            var gun = character.CurrentHoldItemAgent as ItemAgent_Gun;
            if (gun != null && !gun.isActiveAndEnabled) gun = null;
            var rightSocket = FindRightSocket(gun);
            if (_gun != gun || _rightSocket != rightSocket)
            {
                _gun = gun;
                _weight = 0;
                _leftSocket = gun != null ? gun.GetSocket(_settings.LeftHandSocket, false) : null;
                _rightSocket = rightSocket;
                _calibrated = false;
                CalibrateHand();
                if (_rightSocket != null && gun != null)
                {
                    var muzzle = gun.muzzle;
                    _socketToMuzzleRotation = Quaternion.Inverse(_rightSocket.rotation) *
                                              (muzzle ? muzzle.rotation : gun.transform.rotation);
                }
            }

            var active = _settings.Enabled && gun != null && gun.isActiveAndEnabled &&
                         !gun.IsReloading() && !control.GetParameterBool(CustomAnimatorHash.UsingItem) &&
                         character.CurrentAction is not CA_Attack { Running: true } &&
                         character.CurrentAction is not CA_UseItem { Running: true };
            _weight = Mathf.MoveTowards(_weight, active ? _settings.Weight : 0,
                _settings.BlendSpeed * Mathf.Max(0, Time.deltaTime));
            if (gun == null || _weight <= 0 || (!_settings.RightHand && !_settings.LeftHand))
            {
                RestoreBaseline();
                return;
            }

            var direction = character.CurrentAimDirection;
            if (!TwoBoneIk.IsFinite(direction) || direction.sqrMagnitude < 0.000001f)
                direction = _rig.Root.forward;
            direction.Normalize();
            var up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f ? Vector3.up : _rig.Root.forward;
            if (Vector3.Cross(direction, up).sqrMagnitude < 0.000001f) up = Vector3.right;
            var aim = Quaternion.LookRotation(direction, up);
            var hasRight = _rig.TryGetArm(false, out var right);
            var hasLeft = _rig.TryGetArm(true, out var left);
            _baselineRootRotation = _rig.Root.rotation;

            if (_settings.RightHand && hasRight)
            {
                CalibrateHand();
                var length = ArmLength(right);
                var socketRotation = aim * Quaternion.Inverse(_socketToMuzzleRotation);
                var handRotation = _calibrated
                    ? socketRotation * Quaternion.Inverse(_handToSocketRotation)
                    : aim;
                var target = right.UpperPosition + aim * Vector(_settings.RightHandOffset) * length;
                if (_calibrated) target -= handRotation * (_handToSocketOffset * length);
                if (TwoBoneIk.TrySolve(right, target, handRotation, ElbowHint(false, right), out var solved))
                {
                    _rightBaseline = right;
                    _restoreRight = !_externalUpdate;
                    _rig.ApplyArm(false, solved, _weight);
                    IsControllingHands = true;
                }
            }

            if (!_settings.LeftHand || !hasLeft) return;
            if (_externalUpdate) _handler.YsmRuntime?.UpdateIkSockets();
            var gunMatrix = gun.transform.localToWorldMatrix;
            var leftTarget = left.HandPosition;
            var leftRotation = left.HandRotation;
            if (hasRight && _rig.TryGetArm(false, out var appliedRight))
            {
                var handDelta = appliedRight.HandRotation * Quaternion.Inverse(right.HandRotation);
                leftTarget = appliedRight.HandPosition + handDelta * (left.HandPosition - right.HandPosition);
                leftRotation = handDelta * left.HandRotation;
            }

            if (_leftSocket != null) leftTarget = _leftSocket.position;
            leftTarget += gunMatrix.MultiplyVector(Vector(_settings.LeftHandOffset));
            leftRotation *= Quaternion.Euler(Vector(_settings.LeftHandRotation));
            if (!TwoBoneIk.TrySolve(left, leftTarget, leftRotation, ElbowHint(true, left), out var leftSolved)) return;
            _leftBaseline = left;
            _restoreLeft = !_externalUpdate;
            _rig.ApplyArm(true, leftSolved, _weight);
            IsControllingHands = true;
        }

        private Vector3 ElbowHint(bool left, in ArmIkPose pose)
        {
            var localHint = new Vector3(left ? -0.75f : 0.75f, -0.5f, -0.15f);
            return pose.UpperPosition + _rig!.Root.rotation * localHint * ArmLength(pose);
        }

        private void CalibrateHand()
        {
            if (_rig == null || _rightSocket == null || !_rig.TryGetArm(false, out var right)) return;
            var length = ArmLength(right);
            if (length <= 0.000001f) return;
            _handToSocketRotation = Quaternion.Inverse(right.HandRotation) * _rightSocket.rotation;
            _handToSocketOffset = Quaternion.Inverse(right.HandRotation) *
                (_rightSocket.position - right.HandPosition) / length;
            _calibrated = true;
        }

        private Transform? FindRightSocket(ItemAgent_Gun? gun)
        {
            if (_handler == null) return null;
            if (gun != null)
            {
                var rifle = _handler.GetSocketTransform(SocketNames.Rifle);
                if (rifle && gun.transform.IsChildOf(rifle)) return rifle;
                var pistol = _handler.GetSocketTransform(SocketNames.Pistol);
                if (pistol && gun.transform.IsChildOf(pistol)) return pistol;
            }

            return _handler.GetSocketTransform(SocketNames.RightHand);
        }

        private void RestoreBaseline()
        {
            if (_rig != null && _rig.Root)
            {
                var rotation = _rig.Root.rotation * Quaternion.Inverse(_baselineRootRotation);
                if (_restoreRight) RestoreArm(false, _rightBaseline, rotation);
                if (_restoreLeft) RestoreArm(true, _leftBaseline, rotation);
            }

            _restoreRight = false;
            _restoreLeft = false;
        }

        private void RestoreArm(bool left, ArmIkPose pose, Quaternion rootDelta)
        {
            pose.UpperRotation = rootDelta * pose.UpperRotation;
            pose.LowerRotation = rootDelta * pose.LowerRotation;
            pose.HandRotation = rootDelta * pose.HandRotation;
            _rig!.ApplyArm(left, pose, 1);
        }

        private void TakeAutoSync()
        {
            if (_ownsAutoSync || !isActiveAndEnabled || _handler?.OriginalCharacterModel == null ||
                _externalUpdate || (_handler.CurrentModelInfo is { } model && VrmModelSource.IsVrm(model))) return;
            OriginalAutoSyncRightHandRotation = _handler.OriginalCharacterModel.autoSyncRightHandRotation;
            _handler.OriginalCharacterModel.autoSyncRightHandRotation = false;
            _ownsAutoSync = true;
        }

        private void RestoreAutoSync()
        {
            if (_ownsAutoSync && _handler?.OriginalCharacterModel != null)
                _handler.OriginalCharacterModel.autoSyncRightHandRotation = OriginalAutoSyncRightHandRotation;
            _ownsAutoSync = false;
        }

        private void Unsubscribe()
        {
            if (_adapter != null) _adapter.PoseEvaluated -= ApplyPose;
            _adapter = null;
        }

        private static float ArmLength(in ArmIkPose pose)
        {
            return Vector3.Distance(pose.UpperPosition, pose.ElbowPosition) +
                   Vector3.Distance(pose.ElbowPosition, pose.HandPosition);
        }

        private static Vector3 Vector(float[] values)
        {
            return new(values[0], values[1], values[2]);
        }
    }
}
