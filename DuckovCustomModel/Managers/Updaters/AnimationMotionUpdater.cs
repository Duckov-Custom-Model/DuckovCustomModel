using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;

namespace DuckovCustomModel.Managers.Updaters
{
    public class AnimationMotionUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            var character = control.CharacterMainControl;
            if (!control.Initialized || character == null) return;
            var movement = character.movementControl;
            var velocity = character.Velocity;
            var speed = new Vector2(velocity.x, velocity.z).magnitude;
            var moveSpeed = character.AnimationMoveSpeedValue;
            if (movement != null && movement.walkSpeed > .0001f)
                moveSpeed = speed > movement.walkSpeed
                    ? 1 + Mathf.InverseLerp(movement.walkSpeed, movement.runSpeed, speed)
                    : Mathf.Clamp01(speed / movement.walkSpeed);
            moveSpeed = float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed) ? 0 : Mathf.Clamp(moveSpeed, 0, 2);
            var root = control.ModelHandler?.CustomAnimator != null
                ? control.ModelHandler.CustomAnimator.transform
                : control.CharacterModel != null
                    ? control.CharacterModel.transform
                    : character.transform;
            var localVelocity = root.InverseTransformDirection(velocity);
            var direction = new Vector2(localVelocity.x, localVelocity.z);
            direction = direction.sqrMagnitude > .000001f ? direction.normalized : Vector2.zero;
            var previous = new Vector2(control.GetParameterFloat(CustomAnimatorHash.LocomotionX),
                control.GetParameterFloat(CustomAnimatorHash.LocomotionY));
            var blended = Vector2.Lerp(previous, direction * moveSpeed, 1 - Mathf.Exp(-15 * Time.deltaTime));
            control.SetParameterFloat(CustomAnimatorHash.LocomotionX, blended.x);
            control.SetParameterFloat(CustomAnimatorHash.LocomotionY, blended.y);
            var cadence = movement != null && movement.originWalkSpeed > .0001f
                ? movement.walkSpeed / movement.originWalkSpeed
                : 1;
            if (float.IsNaN(cadence) || float.IsInfinity(cadence)) cadence = 1;
            control.SetParameterFloat(CustomAnimatorHash.LocomotionRate,
                Mathf.Lerp(1, Mathf.Clamp(cadence, .1f, 5), Mathf.Clamp01(blended.magnitude)));
            var dash = character.CurrentAction as CA_Dash;
            var rolling = dash is { Running: true };
            control.SetParameterBool(CustomAnimatorHash.RollPose, rolling);
            if (rolling)
            {
                var progress = dash!.GetProgress();
                control.SetParameterFloat(CustomAnimatorHash.RollProgress,
                    progress.total > 0 ? Mathf.Clamp01(progress.current / progress.total) : 0);
            }

            control.SetParameterFloat(CustomAnimatorHash.AttackProgress,
                control is { Attacking: true, AttackTime: > 0 }
                    ? Mathf.Clamp01(control.AttackTimer / control.AttackTime)
                    : 0);
        }
    }
}
