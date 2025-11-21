using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Duckov.UI.DialogueBubbles;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.MonoBehaviours.Animators;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.MonoBehaviours;
using Newtonsoft.Json;
using SodaCraft.Localizations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DuckovCustomModel.Managers
{
    public static class CustomDialogueManager
    {
        private static readonly Dictionary<string, Dictionary<string, DialogueDefinition>> DialogueCache = [];
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            ModelDialogueTrigger.OnDialogueTriggered += OnDialogueTriggered;
            Localization.OnLanguageChangedEvent += OnLanguageChanged;
            _initialized = true;
        }

        public static void Cleanup()
        {
            ModelDialogueTrigger.OnDialogueTriggered -= OnDialogueTriggered;
            Localization.OnLanguageChangedEvent -= OnLanguageChanged;
            _initialized = false;
            DialogueCache.Clear();
        }

        private static void OnLanguageChanged(SystemLanguage language)
        {
            DialogueCache.Clear();
        }

        private static void OnDialogueTriggered(string fileName, string dialogueId, SystemLanguage defaultLanguage,
            Animator animator)
        {
            if (animator == null) return;

            var modelHandler = animator.GetComponentInParent<ModelHandler>();
            if (modelHandler == null || string.IsNullOrEmpty(modelHandler.CurrentModelDirectory)) return;

            var defaultLanguageKey = GetLanguageKey(defaultLanguage);
            var definitions =
                LoadDialogueDefinitions(modelHandler.CurrentModelDirectory!, fileName, defaultLanguageKey);
            if (definitions == null || !definitions.TryGetValue(dialogueId, out var definition)) return;

            var targetTransform = animator.transform;
            var yOffset = 2f;

            if (modelHandler.CharacterMainControl != null)
            {
                targetTransform = modelHandler.CharacterMainControl.transform;
                if (modelHandler.OriginalCharacterModel != null &&
                    modelHandler.OriginalCharacterModel.HelmatSocket != null)
                    yOffset = Vector3.Distance(targetTransform.position,
                        modelHandler.OriginalCharacterModel.HelmatSocket.position) + 0.5f;
            }

            PlayDialogue(definition, targetTransform, yOffset).Forget();
        }

        private static Dictionary<string, DialogueDefinition>? LoadDialogueDefinitions(string modelDirectory,
            string fileName, string defaultLanguage)
        {
            var currentLanguage = GetLanguageKey();

            var filePath = Path.Combine(modelDirectory, $"{fileName}_{currentLanguage}.json");
            if (!File.Exists(filePath)) filePath = Path.Combine(modelDirectory, $"{fileName}_{defaultLanguage}.json");

            if (!File.Exists(filePath)) return null;

            if (DialogueCache.TryGetValue(filePath, out var cachedDefs)) return cachedDefs;

            try
            {
                var json = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<List<DialogueDefinition>>(json);
                if (list == null) return null;

                Dictionary<string, DialogueDefinition> dict = [];
                foreach (var def in list.Where(def => !string.IsNullOrEmpty(def.Id)))
                    dict[def.Id] = def;

                DialogueCache[filePath] = dict;
                return dict;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load dialogue file {filePath}: {ex.Message}");
                return null;
            }
        }

        private static string GetLanguageKey()
        {
            return GetLanguageKey(LocalizationManager.CurrentLanguage);
        }

        private static string GetLanguageKey(SystemLanguage language)
        {
            return language is SystemLanguage.Chinese or SystemLanguage.ChineseSimplified
                ? "Chinese"
                : language.ToString();
        }

        private static async UniTaskVoid PlayDialogue(DialogueDefinition def, Transform target, float yOffset)
        {
            if (def.Texts is not { Length: > 0 }) return;

            if (def.Mode == DialoguePlayMode.Continuous)
            {
                foreach (var text in def.Texts)
                {
                    if (target == null) return;
                    await DialogueBubblesManager.Show(text, target, yOffset, duration: def.Duration);
                }
            }
            else
            {
                var text = SelectText(def);
                if (target != null) await DialogueBubblesManager.Show(text, target, yOffset, duration: def.Duration);
            }
        }

        private static string SelectText(DialogueDefinition def)
        {
            if (def.Texts.Length == 1) return def.Texts[0];

            switch (def.Mode)
            {
                case DialoguePlayMode.Sequential:
                    var text = def.Texts[def.CurrentIndex];
                    def.CurrentIndex = (def.CurrentIndex + 1) % def.Texts.Length;
                    return text;

                case DialoguePlayMode.Random:
                    return def.Texts[Random.Range(0, def.Texts.Length)];

                case DialoguePlayMode.RandomNoRepeat:
                    if (def.RemainingIndices.Count == 0)
                        for (var i = 0; i < def.Texts.Length; i++)
                            def.RemainingIndices.Add(i);

                    var randIndex = Random.Range(0, def.RemainingIndices.Count);
                    var textIndex = def.RemainingIndices[randIndex];
                    def.RemainingIndices.RemoveAt(randIndex);

                    return def.Texts[textIndex];

                default:
                    return def.Texts[0];
            }
        }
    }
}
