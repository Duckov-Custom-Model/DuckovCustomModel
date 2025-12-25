using Duckov;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class ActionStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var currentAction = control.CharacterMainControl.CurrentAction;
            var isActionRunning = false;
            var actionProgress = 0.0f;
            var actionPriority = 0;
            if (currentAction != null)
            {
                isActionRunning = currentAction.Running;
                if (currentAction is IProgress progressAction)
                    actionProgress = progressAction.GetProgress().progress;
                actionPriority = (int)currentAction.ActionPriority();
            }

            control.SetParameterBool(CustomAnimatorHash.ActionRunning, isActionRunning);
            control.SetParameterFloat(CustomAnimatorHash.ActionProgress, actionProgress);
            control.SetParameterInteger(CustomAnimatorHash.ActionPriority, actionPriority);
        }
    }
}
