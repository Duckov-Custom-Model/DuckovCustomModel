using System;
using DuckovCustomModel.Core.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DuckovCustomModel.Core.MonoBehaviours.Animators
{
    public class ModelSoundTrigger : StateMachineBehaviour
    {
        public enum PlayOrder
        {
            Random,
            Sequential,
        }

        [Tooltip("Sound tags to play. Multiple tags can be specified.")]
        public string[] soundTags = [];

        [Tooltip("How to select from multiple tags: Random or Sequential.")]
        public PlayOrder playOrder = PlayOrder.Random;

        [Tooltip("Play mode: Normal - allows multiple sounds; StopPrevious - stops previous sounds; " +
                 "SkipIfPlaying - skips if already playing; UseTempObject - creates temporary object at position " +
                 "(prevents sound stopping on character death, but lower performance).")]
        public SoundPlayMode playMode = SoundPlayMode.Normal;

        [Tooltip("Event name for sound playback management. If empty, a default name will be generated.")]
        public string eventName = string.Empty;

        private int _sequentialIndex;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (soundTags is not { Length : > 0 }) return;

            var validTags = Array.FindAll(soundTags, tag => !string.IsNullOrWhiteSpace(tag));
            if (validTags.Length == 0) return;

            string selectedTag;
            if (playOrder == PlayOrder.Random)
            {
                selectedTag = validTags[Random.Range(0, validTags.Length)];
            }
            else
            {
                selectedTag = validTags[_sequentialIndex];
                _sequentialIndex = (_sequentialIndex + 1) % validTags.Length;
            }

            selectedTag = selectedTag.ToLowerInvariant().Trim();
            var finalEventName = string.IsNullOrWhiteSpace(eventName)
                ? "CustomModelSoundTrigger"
                : eventName;

            OnSoundTriggered?.Invoke(selectedTag, finalEventName, playMode, animator);
        }

        public static event Action<string, string, SoundPlayMode, Animator>? OnSoundTriggered;
    }
}
