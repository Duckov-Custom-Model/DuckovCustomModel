using System;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class CharacterStatusUpdater : IAnimatorParameterUpdater
    {
        private static readonly Func<CharacterMainControl, bool>? ReadInvisible =
            CreateOptionalBooleanGetter("Invisible");

        private static readonly Func<CharacterMainControl, bool>? ReadBodyInWater =
            CreateOptionalBooleanGetter("BodyInWater");

        private static readonly Func<CharacterMainControl, bool>? ReadHeadInWater =
            CreateOptionalBooleanGetter("HeadInWater");

        private static readonly Func<CharacterMainControl, bool>? ReadFootInWater =
            CreateOptionalBooleanGetter("FootInWater");

        private static readonly Func<CharacterMainControl, bool>? ReadSwimming =
            CreateOptionalBooleanGetter("Swimming");

        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var character = control.CharacterMainControl;
            var hidden = character.Hidden;
            control.SetParameterBool(CustomAnimatorHash.Hidden, hidden);
            control.SetParameterBool(CustomAnimatorHash.Invisible, GetOptionalBoolean(character, ReadInvisible));
            control.SetParameterBool(CustomAnimatorHash.BodyInWater, GetOptionalBoolean(character, ReadBodyInWater));
            control.SetParameterBool(CustomAnimatorHash.HeadInWater, GetOptionalBoolean(character, ReadHeadInWater));
            control.SetParameterBool(CustomAnimatorHash.FootInWater, GetOptionalBoolean(character, ReadFootInWater));
            control.SetParameterBool(CustomAnimatorHash.Swimming, GetOptionalBoolean(character, ReadSwimming));

            if (character.Health != null)
            {
                var currentHealth = character.Health.CurrentHealth;
                var maxHealth = character.Health.MaxHealth;
                var healthRate = maxHealth > 0 ? currentHealth / maxHealth : 0.0f;
                control.SetParameterFloat(CustomAnimatorHash.Health, currentHealth);
                control.SetParameterFloat(CustomAnimatorHash.MaxHealth, maxHealth);
                control.SetParameterFloat(CustomAnimatorHash.HealthRate, healthRate);
            }
            else
            {
                control.SetParameterFloat(CustomAnimatorHash.Health, 20f);
                control.SetParameterFloat(CustomAnimatorHash.MaxHealth, 20f);
                control.SetParameterFloat(CustomAnimatorHash.HealthRate, 1.0f);
            }

            var currentEnergy = character.CurrentEnergy;
            var maxEnergy = character.MaxEnergy;
            control.SetParameterFloat(CustomAnimatorHash.Energy, currentEnergy);
            control.SetParameterFloat(CustomAnimatorHash.MaxEnergy, maxEnergy);
            var energyRate = maxEnergy > 0 ? currentEnergy / maxEnergy : 1f;
            control.SetParameterFloat(CustomAnimatorHash.EnergyRate, energyRate);
            control.SetParameterFloat(CustomAnimatorHash.FoodLevel,
                Math.Max(0f, Math.Min(20f, 20f * energyRate)));

            var currentWater = character.CurrentWater;
            var maxWater = character.MaxWater;
            control.SetParameterFloat(CustomAnimatorHash.Water, currentWater);
            control.SetParameterFloat(CustomAnimatorHash.MaxWater, maxWater);
            if (maxWater > 0)
            {
                var waterRate = currentWater / maxWater;
                control.SetParameterFloat(CustomAnimatorHash.WaterRate, waterRate);
            }
            else
            {
                control.SetParameterFloat(CustomAnimatorHash.WaterRate, 1.0f);
            }

            var totalWeight = character.CharacterItem.TotalWeight;
            if (character.carryAction.Running)
                totalWeight += character.carryAction.GetWeight();

            var maxWeight = character.MaxWeight;
            var weightRate = maxWeight > 0 ? totalWeight / maxWeight : 0f;
            control.SetParameterFloat(CustomAnimatorHash.Weight, totalWeight);
            control.SetParameterFloat(CustomAnimatorHash.MaxWeight, maxWeight);
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

        private static Func<CharacterMainControl, bool>? CreateOptionalBooleanGetter(string name)
        {
            var type = typeof(CharacterMainControl);
            var getter = type.GetProperty(name)?.GetGetMethod();
            if (getter != null && getter.ReturnType == typeof(bool))
                return (Func<CharacterMainControl, bool>)Delegate.CreateDelegate(
                    typeof(Func<CharacterMainControl, bool>), getter);
            var field = type.GetField(name);
            return field != null && field.FieldType == typeof(bool) ? target => (bool)field.GetValue(target)! : null;
        }

        private static bool GetOptionalBoolean(CharacterMainControl character, Func<CharacterMainControl, bool>? getter)
        {
            if (getter == null) return false;
            try
            {
                return getter(character);
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
    }
}
