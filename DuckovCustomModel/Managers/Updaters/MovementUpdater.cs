using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class MovementUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            control.SetParameterFloat(CustomAnimatorHash.MoveSpeed,
                control.CharacterMainControl.AnimationMoveSpeedValue);

            var moveDirectionValue = control.CharacterMainControl.AnimationLocalMoveDirectionValue;
            control.SetParameterFloat(CustomAnimatorHash.MoveDirX, moveDirectionValue.x);
            control.SetParameterFloat(CustomAnimatorHash.MoveDirY, moveDirectionValue.y);

            control.SetParameterBool(CustomAnimatorHash.Grounded, control.CharacterMainControl.IsOnGround);

            var movementControl = control.CharacterMainControl.movementControl;
            control.SetParameterBool(CustomAnimatorHash.IsMoving, movementControl.Moving);
            control.SetParameterBool(CustomAnimatorHash.IsRunning, movementControl.Running);

            var dashing = control.CharacterMainControl.Dashing;
            if (dashing && !control.HasAnimationIfDashCanControl && control.CharacterMainControl.DashCanControl)
                dashing = false;
            control.SetParameterBool(CustomAnimatorHash.Dashing, dashing);
        }
    }
}
