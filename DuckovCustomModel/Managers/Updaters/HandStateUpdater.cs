using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class HandStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;

            var handState = 0;
            var rightHandOut = true;

            if (ctx.HoldAgent == null || !ctx.HoldAgent.isActiveAndEnabled)
                ctx.HoldAgent = ctx.CharacterMainControl.CurrentHoldItemAgent;
            else
                handState = (int)ctx.HoldAgent.handAnimationType;
            if (ctx.CharacterMainControl.carryAction.Running)
                handState = -1;

            if (ctx.HoldAgent == null || !ctx.HoldAgent.gameObject.activeSelf ||
                ctx.CharacterMainControl.reloadAction.Running)
                rightHandOut = false;

            customControl.SetParameterInteger(CustomAnimatorHash.HandState, handState);
            customControl.SetParameterBool(CustomAnimatorHash.RightHandOut, rightHandOut);
        }
    }
}
