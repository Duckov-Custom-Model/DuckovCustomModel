using System;
using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Animators
{
    public class ModelSoundStopTrigger : StateMachineBehaviour
    {
        [Tooltip("If true, stop all playing sounds. If false, stop the sound specified by eventName.")]
        public bool stopAllSounds;

        [Tooltip(
            "If true, use built-in event name directly (e.g., 'idle') without 'CustomModelSoundTrigger:' prefix. " +
            "WARNING: Only use this for built-in event names like 'idle'. For custom triggers, leave this false.")]
        public bool useBuiltInEventName;

        [Tooltip("Event name of the sound to stop. If empty, a default name will be used (same as ModelSoundTrigger).")]
        public string eventName = string.Empty;

        [Tooltip("If true, stop the sound when entering this state. If false, stop when exiting.")]
        public bool stopOnEnter = true;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stopOnEnter) TriggerStopSound(stopAllSounds, useBuiltInEventName, eventName, animator);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!stopOnEnter) TriggerStopSound(stopAllSounds, useBuiltInEventName, eventName, animator);
        }

        private static void TriggerStopSound(bool stopAll, bool useBuiltIn, string eventName, Animator animator)
        {
            if (stopAll)
            {
                OnSoundStopTriggered?.Invoke(string.Empty, animator);
            }
            else if (useBuiltIn)
            {
                if (string.IsNullOrWhiteSpace(eventName))
                {
                    Debug.LogWarning("ModelSoundStopTrigger: useBuiltInEventName is true but eventName is empty. " +
                                     "Please specify a built-in event name (e.g., 'idle').");
                    return;
                }

                OnSoundStopTriggered?.Invoke(eventName, animator);
            }
            else
            {
                var targetEvent = string.IsNullOrWhiteSpace(eventName)
                    ? "CustomModelSoundTrigger"
                    : eventName;
                var finalEventName = $"CustomModelSoundTrigger:{targetEvent}";
                OnSoundStopTriggered?.Invoke(finalEventName, animator);
            }
        }

        public static event Action<string, Animator>? OnSoundStopTriggered;
    }
}
