using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;

namespace DuckovCustomModel.Managers.Updaters
{
    public class AttackLayerWeightUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Attacking)
            {
                if (ctx.AttackWeight <= 0) return;
                ctx.AttackWeight = 0;
                customControl.SetMeleeAttackLayerWeight(ctx.AttackWeight);
                return;
            }

            ctx.AttackTimer += Time.deltaTime;
            var attackTime = ctx.AttackTime;
            var attackProgress = attackTime > 0 ? Mathf.Clamp01(ctx.AttackTimer / attackTime) : 0.0f;
            ctx.AttackWeight = ctx.AttackLayerWeightCurve?.Evaluate(attackProgress) ?? 0.0f;
            if (ctx.AttackTimer >= attackTime)
            {
                ctx.Attacking = false;
                ctx.AttackWeight = 0.0f;
            }

            customControl.SetMeleeAttackLayerWeight(ctx.AttackWeight);
        }
    }
}
