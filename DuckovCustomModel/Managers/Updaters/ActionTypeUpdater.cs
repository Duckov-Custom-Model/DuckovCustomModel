using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class ActionTypeUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var currentAction = control.CharacterMainControl.CurrentAction;
            var actionType = -1;
            if (currentAction != null) actionType = CharacterActionDefinitions.GetActionTypeId(currentAction.GetType());

            control.SetParameterInteger(CustomAnimatorHash.ActionType, actionType);

            UpdateActionSubParameters(control, currentAction);
        }

        private static void UpdateActionSubParameters(CustomAnimatorControl control, CharacterActionBase? action)
        {
            var fishingRodTypeID = 0;
            var baitTypeID = 0;
            var useItemTypeID = 0;

            switch (action)
            {
                case Action_Fishing fishingAction:
                {
                    var fishingRod = fishingAction.fishingRod;
                    var bait = fishingAction.bait;
                    fishingRodTypeID = fishingRod != null ? fishingRod.selfAgent.Item.TypeID : 0;
                    baitTypeID = bait != null ? bait.TypeID : 0;
                    break;
                }
                case Action_FishingV2 fishingV2Action:
                {
                    var fishingRod = fishingV2Action.rod;
                    var bait = fishingV2Action.baitItem;
                    fishingRodTypeID = fishingRod != null ? fishingRod.selfAgent.Item.TypeID : 0;
                    baitTypeID = bait != null ? bait.TypeID : 0;
                    break;
                }
                case CA_UseItem caUseItem:
                {
                    useItemTypeID = caUseItem.item != null ? caUseItem.item.TypeID : 0;
                    break;
                }
            }

            control.SetParameterInteger(CustomAnimatorHash.ActionFishingRodTypeID, fishingRodTypeID);
            control.SetParameterInteger(CustomAnimatorHash.ActionBaitTypeID, baitTypeID);
            control.SetParameterInteger(CustomAnimatorHash.ActionUseItemTypeID, useItemTypeID);
        }
    }
}
