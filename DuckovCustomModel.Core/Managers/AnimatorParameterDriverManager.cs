using System.Linq;
using DuckovCustomModel.Core.MonoBehaviours.Animators;
using UnityEngine;

namespace DuckovCustomModel.Core.Managers
{
    public static class AnimatorParameterDriverManager
    {
        public static void InitializeDriver(ModelParameterDriver parameterDriver, Animator animator)
        {
            if (parameterDriver.Initialized) return;

            parameterDriver.Initialized = true;

            var enableParameters = parameterDriver.Parameters
                .Where(parameter => InitializeParameter(parameter, animator)).ToArray();

            parameterDriver.Parameters = enableParameters;
            parameterDriver.IsEnabled = parameterDriver.Parameters.Length > 0;
        }

        public static void ApplyParameter(ModelParameterDriver.Parameter parameter, Animator animator)
        {
            switch (parameter.type)
            {
                case ModelParameterDriver.ChangeType.Set:
                {
                    ApplyParameterAsSet(parameter, animator);
                    break;
                }
                case ModelParameterDriver.ChangeType.Add:
                {
                    ApplyParameterAsAdd(parameter, animator);
                    break;
                }
                case ModelParameterDriver.ChangeType.Random:
                {
                    ApplyParameterAsRandom(parameter, animator);
                    break;
                }
                case ModelParameterDriver.ChangeType.Copy:
                {
                    ApplyParameterAsCopy(parameter, animator);
                    break;
                }
            }
        }

        private static bool InitializeParameter(ModelParameterDriver.Parameter parameter, Animator animator)
        {
            if (string.IsNullOrEmpty(parameter.name)) return false;

            var parameterExists = animator.parameters
                .Any(p => p.name == parameter.name);

            if (!parameterExists) return false;

            var destParam = animator.parameters
                .FirstOrDefault(p => p.name == parameter.name);
            if (destParam == null) return false;

            parameter.DestParam = destParam;

            if (parameter.type != ModelParameterDriver.ChangeType.Copy) return true;
            {
                if (string.IsNullOrEmpty(parameter.source)) return false;

                var sourceParam = animator.parameters
                    .FirstOrDefault(p => p.name == parameter.source);
                if (sourceParam == null) return false;

                parameter.SourceParam = sourceParam;
            }

            return true;
        }

        private static void ApplyParameterAsSet(ModelParameterDriver.Parameter parameter, Animator animator)
        {
            if (parameter.DestParam is not AnimatorControllerParameter targetParam)
                return;

            switch (targetParam.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(targetParam.name, parameter.value);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(targetParam.name, (int)parameter.value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(targetParam.name, parameter.value > 0f);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(targetParam.name);
                    break;
            }
        }

        private static void ApplyParameterAsAdd(ModelParameterDriver.Parameter parameter, Animator animator)
        {
            if (parameter.DestParam is not AnimatorControllerParameter targetParam)
                return;

            switch (targetParam.type)
            {
                case AnimatorControllerParameterType.Float:
                {
                    var currentValue = animator.GetFloat(targetParam.name);
                    animator.SetFloat(targetParam.name, currentValue + parameter.value);
                    break;
                }
                case AnimatorControllerParameterType.Int:
                {
                    var currentValue = animator.GetInteger(targetParam.name);
                    animator.SetInteger(targetParam.name, currentValue + (int)parameter.value);
                    break;
                }
            }
        }

        private static void ApplyParameterAsRandom(ModelParameterDriver.Parameter parameter, Animator animator)
        {
            if (parameter.DestParam is not AnimatorControllerParameter targetParam)
                return;

            switch (targetParam.type)
            {
                case AnimatorControllerParameterType.Float:
                    var randomFloat = Random.Range(parameter.valueMin, parameter.valueMax);
                    animator.SetFloat(targetParam.name, randomFloat);
                    break;
                case AnimatorControllerParameterType.Int:
                    var randomInt = Random.Range((int)parameter.valueMin, (int)parameter.valueMax + 1);
                    animator.SetInteger(targetParam.name, randomInt);
                    break;
                case AnimatorControllerParameterType.Bool:
                    var randomBool = Random.value < parameter.chance;
                    animator.SetBool(targetParam.name, randomBool);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    if (Random.value < parameter.chance)
                        animator.SetTrigger(targetParam.name);
                    break;
            }
        }

        private static void ApplyParameterAsCopy(ModelParameterDriver.Parameter parameter, Animator animator)
        {
            if (parameter.DestParam is not AnimatorControllerParameter targetParam)
                return;

            if (parameter.SourceParam is not AnimatorControllerParameter sourceParam)
                return;

            if (sourceParam.type == AnimatorControllerParameterType.Trigger)
                return;

            var sourceValue = sourceParam.type switch
            {
                AnimatorControllerParameterType.Float => animator.GetFloat(sourceParam.name),
                AnimatorControllerParameterType.Int => animator.GetInteger(sourceParam.name),
                AnimatorControllerParameterType.Bool => animator.GetBool(sourceParam.name) ? 1f : 0f,
                _ => 0f,
            };

            var finalValue = sourceValue;
            if (parameter.convertRange)
            {
                var sourceMin = parameter.sourceMin;
                var sourceMax = parameter.sourceMax;
                var targetMin = parameter.destMin;
                var targetMax = parameter.destMax;

                if (Mathf.Abs(sourceMax - sourceMin) > Mathf.Epsilon)
                    finalValue = targetMin + (sourceValue - sourceMin) * (targetMax - targetMin) /
                        (sourceMax - sourceMin);
            }

            switch (targetParam.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(targetParam.name, finalValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(targetParam.name, (int)finalValue);
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(targetParam.name, finalValue > 0f);
                    break;
            }
        }
    }
}
