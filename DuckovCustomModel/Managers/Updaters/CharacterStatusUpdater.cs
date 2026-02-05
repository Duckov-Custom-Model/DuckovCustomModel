using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class CharacterStatusUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var hidden = control.CharacterMainControl.Hidden;
            control.SetParameterBool(CustomAnimatorHash.Hidden, hidden);

            if (control.CharacterMainControl.Health != null)
            {
                var currentHealth = control.CharacterMainControl.Health.CurrentHealth;
                var maxHealth = control.CharacterMainControl.Health.MaxHealth;
                var healthRate = maxHealth > 0 ? currentHealth / maxHealth : 0.0f;
                control.SetParameterFloat(CustomAnimatorHash.HealthRate, healthRate);
            }
            else
            {
                control.SetParameterFloat(CustomAnimatorHash.HealthRate, 1.0f);
            }

            var currentWater = control.CharacterMainControl.CurrentWater;
            var maxWater = control.CharacterMainControl.MaxWater;
            if (maxWater > 0)
            {
                var waterRate = currentWater / maxWater;
                control.SetParameterFloat(CustomAnimatorHash.WaterRate, waterRate);
            }
            else
            {
                control.SetParameterFloat(CustomAnimatorHash.WaterRate, 1.0f);
            }

            var totalWeight = control.CharacterMainControl.CharacterItem.TotalWeight;
            if (control.CharacterMainControl.carryAction.Running)
                totalWeight += control.CharacterMainControl.carryAction.GetWeight();

            var weightRate = totalWeight / control.CharacterMainControl.MaxWeight;
            control.SetParameterFloat(CustomAnimatorHash.WeightRate, weightRate);

            int weightState;
            if (!LevelManager.Instance.IsRaidMap)
                weightState = (int)CharacterMainControl.WeightStates.normal;
            else
                weightState = totalWeight switch
                {
                    > 1 => (int)CharacterMainControl.WeightStates.overWeight,
                    > 0.75f => (int)CharacterMainControl.WeightStates.superHeavy,
                    > 0.25f => (int)CharacterMainControl.WeightStates.normal,
                    _ => (int)CharacterMainControl.WeightStates.light,
                };
            control.SetParameterInteger(CustomAnimatorHash.WeightState, weightState);

            control.SetParameterBool(CustomAnimatorHash.Sleeping, control.CharacterMainControl.Sleeping);

            control.SetParameterBool(CustomAnimatorHash.IsVehicle, control.CharacterMainControl.isVehicle);

            var isControlling = false;
            var isControllingVehicle = false;
            if (control.CharacterMainControl.controlOtherCharacterAction != null)
            {
                var haveTargetCharacter =
                    control.CharacterMainControl.controlOtherCharacterAction.targetCharacter != null;
                isControlling = haveTargetCharacter &&
                                control.CharacterMainControl.controlOtherCharacterAction.Running;
                isControllingVehicle = isControlling &&
                                       control.CharacterMainControl.controlOtherCharacterAction.vehicleControl;
            }

            control.SetParameterBool(CustomAnimatorHash.IsControllingOtherCharacter, isControlling);
            control.SetParameterBool(CustomAnimatorHash.IsControllingVehicle, isControllingVehicle);
            control.SetParameterInteger(CustomAnimatorHash.RidingVehicleType,
                control.CharacterMainControl.ridingVehicleType);

            var currentControlling = LevelManager.Instance.ControllingCharacter;
            var isCurrentlyControlling =
                currentControlling != null && currentControlling == control.CharacterMainControl;
            control.SetParameterBool(CustomAnimatorHash.IsPlayerControlling, isCurrentlyControlling);
        }
    }
}
