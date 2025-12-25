using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class HandStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var handState = 0;
            var rightHandOut = true;

            var holdAgent = control.HoldAgent;
            if (holdAgent == null || !holdAgent.isActiveAndEnabled)
                holdAgent = control.CharacterMainControl.CurrentHoldItemAgent;

            if (holdAgent != null && holdAgent.isActiveAndEnabled)
                handState = (int)holdAgent.handAnimationType;

            if (control.CharacterMainControl.carryAction.Running)
                handState = -1;

            if (holdAgent == null || !holdAgent.gameObject.activeSelf ||
                control.CharacterMainControl.reloadAction.Running)
                rightHandOut = false;

            control.SetParameterInteger(CustomAnimatorHash.HandState, handState);
            control.SetParameterBool(CustomAnimatorHash.RightHandOut, rightHandOut);
        }
    }
}
