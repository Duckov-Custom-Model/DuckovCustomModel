using System;
using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Animators
{
    public class ModelDialogueTrigger : StateMachineBehaviour
    {
        [Tooltip("The filename of the dialogue definition file (without extension).")]
        public string fileName = string.Empty;

        [Tooltip("The ID of the dialogue to trigger defined in the dialogue configuration file.")]
        public string dialogueId = string.Empty;

        [Tooltip("The default fallback language if the current language file is missing.")]
        public SystemLanguage defaultLanguage = SystemLanguage.English;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!string.IsNullOrEmpty(dialogueId) && !string.IsNullOrEmpty(fileName))
                OnDialogueTriggered?.Invoke(fileName, dialogueId, defaultLanguage, animator);
        }

        public static event Action<string, string, SystemLanguage, Animator>? OnDialogueTriggered;
    }
}
