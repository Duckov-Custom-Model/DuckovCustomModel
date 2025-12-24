using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class MovementUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;

            customControl.SetParameterFloat(CustomAnimatorHash.MoveSpeed,
                ctx.CharacterMainControl.AnimationMoveSpeedValue);

            var moveDirectionValue = ctx.CharacterMainControl.AnimationLocalMoveDirectionValue;
            customControl.SetParameterFloat(CustomAnimatorHash.MoveDirX, moveDirectionValue.x);
            customControl.SetParameterFloat(CustomAnimatorHash.MoveDirY, moveDirectionValue.y);

            customControl.SetParameterBool(CustomAnimatorHash.Grounded, ctx.CharacterMainControl.IsOnGround);

            var movementControl = ctx.CharacterMainControl.movementControl;
            customControl.SetParameterBool(CustomAnimatorHash.IsMoving, movementControl.Moving);
            customControl.SetParameterBool(CustomAnimatorHash.IsRunning, movementControl.Running);

            var dashing = ctx.CharacterMainControl.Dashing;
            if (dashing && !ctx.HasAnimationIfDashCanControl && ctx.CharacterMainControl.DashCanControl)
                dashing = false;
            customControl.SetParameterBool(CustomAnimatorHash.Dashing, dashing);
        }
    }
}
