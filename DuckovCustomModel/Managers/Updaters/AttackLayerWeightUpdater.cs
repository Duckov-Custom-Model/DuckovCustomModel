using DuckovCustomModel.MonoBehaviours;
using UnityEngine;

namespace DuckovCustomModel.Managers.Updaters
{
    public class AttackLayerWeightUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Attacking)
            {
                if (control.AttackWeight <= 0) return;
                control.AttackWeight = 0;
                control.SetMeleeAttackLayerWeight(control.AttackWeight);
                return;
            }

            control.AttackTimer += Time.deltaTime;
            var attackTime = control.AttackTime;
            var attackProgress = attackTime > 0 ? Mathf.Clamp01(control.AttackTimer / attackTime) : 0.0f;
            control.AttackWeight = control.AttackLayerWeightCurve?.Evaluate(attackProgress) ?? 0.0f;
            if (control.AttackTimer >= attackTime)
            {
                control.Attacking = false;
                control.AttackWeight = 0.0f;
            }

            control.SetMeleeAttackLayerWeight(control.AttackWeight);
        }
    }
}
