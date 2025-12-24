using Duckov;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class ActionStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;

            var currentAction = ctx.CharacterMainControl.CurrentAction;
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

            customControl.SetParameterBool(CustomAnimatorHash.ActionRunning, isActionRunning);
            customControl.SetParameterFloat(CustomAnimatorHash.ActionProgress, actionProgress);
            customControl.SetParameterInteger(CustomAnimatorHash.ActionPriority, actionPriority);
        }
    }
}
