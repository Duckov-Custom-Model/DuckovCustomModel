using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    internal sealed class TargetListItem : MonoBehaviour
    {
        private TextMeshProUGUI _assignment = null!;
        private Button _button = null!;
        private TextMeshProUGUI _details = null!;
        private GameObject _selection = null!;
        private TextMeshProUGUI _title = null!;

        internal string TargetId { get; private set; } = string.Empty;

        internal void Initialize()
        {
            _button = GetComponent<Button>();
            _selection = UIFactory.CreateImage("Selection", transform, new Color(.94f, .97f, 1));
            UIFactory.SetupRectTransform(_selection, Vector2.zero, new(0, 1),
                offsetMin: new(0, 8), offsetMax: new(4, -8));
            _selection.GetComponent<Image>().raycastTarget = false;
            _title = CreateLabel("Name", 18, new(12, -34), new(-10, -2));
            _assignment = CreateLabel("Assignment", 14, new(12, -68), new(-10, -36));
            _title.alignment = TextAlignmentOptions.BottomLeft;
            _assignment.alignment = TextAlignmentOptions.TopLeft;
            _title.fontStyle = FontStyles.Bold;
            var tooltip = UIFactory.CreateImage("TargetDetails", transform, new Color(.04f, .06f, .08f, .98f));
            UIFactory.SetupRectTransform(tooltip, new(.5f, .5f), new(.5f, .5f), new(320, 100), pivot: new(0, 1));
            tooltip.GetComponent<Image>().raycastTarget = false;
            var details = UIFactory.CreateText("Text", tooltip.transform, "", 13, UITheme.Text, TextAnchor.UpperLeft);
            UIFactory.SetupRectTransform(details, Vector2.zero, Vector2.one,
                offsetMin: new(7, 7), offsetMax: new(-7, -7));
            _details = details.GetComponent<TextMeshProUGUI>();
            _details.richText = false;
            _details.enableWordWrapping = true;
            _details.overflowMode = TextOverflowModes.Ellipsis;
            tooltip.SetActive(false);
            gameObject.AddComponent<UIItemTooltip>().Initialize(tooltip, _details);
        }

        private TextMeshProUGUI CreateLabel(string name, int size, Vector2 min, Vector2 max)
        {
            var label = UIFactory.CreateText(name, transform, "", size, UITheme.Text);
            UIFactory.SetupRectTransform(label, new(0, 1), Vector2.one, offsetMin: min, offsetMax: max);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = true;
            text.fontSizeMin = size - 2;
            text.fontSizeMax = size;
            text.richText = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        internal void Bind(TargetInfo target, string modelName)
        {
            TargetId = target.Id;
            _selection.SetActive(target.IsSelected);
            _button.image.color = Color.white;
            var colors = _button.colors;
            var stateColor = (target.HasModel, target.HasFallbackModel, target.IsExtension()) switch
            {
                (true, _, true) => new Color(.10f, .27f, .29f, 1),
                (true, _, false) => new Color(.12f, .29f, .20f, 1),
                (false, true, true) => new Color(.18f, .27f, .37f, 1),
                (false, true, false) => new Color(.26f, .20f, .36f, 1),
                (false, false, true) => new Color(.19f, .24f, .28f, 1),
                _ => new Color(.16f, .23f, .34f, 1),
            };
            colors.normalColor = stateColor;
            colors.highlightedColor = Color.Lerp(stateColor, Color.white, .04f);
            colors.selectedColor = stateColor;
            colors.pressedColor = Color.Lerp(stateColor, Color.black, .12f);
            colors.fadeDuration = .1f;
            _button.colors = colors;
            _title.text = target.DisplayName;
            _title.color = new(.97f, .97f, .97f, 1);
            var assignment = target.HasModel ? modelName
                : target.HasFallbackModel ? $"{Localization.TargetInherited}: {modelName}"
                : Localization.TargetOriginal;
            _assignment.text = assignment;
            _assignment.color = new(.84f, .86f, .89f, 1);
            _details.text = $"{target.DisplayName}\n{assignment}\n\n{Localization.TargetType} {target.TargetTypeId}"
                            + (target.HasModel || target.HasFallbackModel
                                ? $"\nID: {(target.HasModel ? target.UsingModel : target.UsingFallbackModel)}"
                                : "")
                            + $"\n\n{Localization.TargetCopyHint}";
        }
    }
}
