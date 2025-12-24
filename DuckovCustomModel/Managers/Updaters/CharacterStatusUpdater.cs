using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class CharacterStatusUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;

            var hidden = ctx.CharacterMainControl.Hidden;
            customControl.SetParameterBool(CustomAnimatorHash.Hidden, hidden);

            if (ctx.CharacterMainControl.Health != null)
            {
                var currentHealth = ctx.CharacterMainControl.Health.CurrentHealth;
                var maxHealth = ctx.CharacterMainControl.Health.MaxHealth;
                var healthRate = maxHealth > 0 ? currentHealth / maxHealth : 0.0f;
                customControl.SetParameterFloat(CustomAnimatorHash.HealthRate, healthRate);
            }
            else
            {
                customControl.SetParameterFloat(CustomAnimatorHash.HealthRate, 1.0f);
            }

            var currentWater = ctx.CharacterMainControl.CurrentWater;
            var maxWater = ctx.CharacterMainControl.MaxWater;
            if (maxWater > 0)
            {
                var waterRate = currentWater / maxWater;
                customControl.SetParameterFloat(CustomAnimatorHash.WaterRate, waterRate);
            }
            else
            {
                customControl.SetParameterFloat(CustomAnimatorHash.WaterRate, 1.0f);
            }

            var totalWeight = ctx.CharacterMainControl.CharacterItem.TotalWeight;
            if (ctx.CharacterMainControl.carryAction.Running)
                totalWeight += ctx.CharacterMainControl.carryAction.GetWeight();

            var weightRate = totalWeight / ctx.CharacterMainControl.MaxWeight;
            customControl.SetParameterFloat(CustomAnimatorHash.WeightRate, weightRate);

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
            customControl.SetParameterInteger(CustomAnimatorHash.WeightState, weightState);
        }
    }
}
