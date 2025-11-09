using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Base
{
    public static class UIFactory
    {
        public static GameObject CreateImage(string name, Transform parent, Color? color = null)
        {
            var obj = new GameObject(name, typeof(Image));
            obj.transform.SetParent(parent, false);

            if (!color.HasValue) return obj;
            var image = obj.GetComponent<Image>();
            image.color = color.Value;

            return obj;
        }

        public static GameObject CreateText(string name, Transform parent, string text, int fontSize = 14,
            Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle fontStyle = FontStyle.Normal)
        {
            var obj = new GameObject(name, typeof(Text));
            obj.transform.SetParent(parent, false);
            var textComponent = obj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = color ?? Color.white;
            textComponent.alignment = alignment;
            textComponent.fontStyle = fontStyle;
            return obj;
        }

        public static GameObject CreateButton(string name, Transform parent, UnityAction? onClick = null,
            Color? backgroundColor = null)
        {
            var obj = new GameObject(name, typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            if (backgroundColor.HasValue)
            {
                var image = obj.GetComponent<Image>();
                image.color = backgroundColor.Value;
            }

            var button = obj.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);

            return obj;
        }

        public static InputField CreateInputField(string name, Transform parent, string placeholder = "")
        {
            var inputObj = new GameObject(name, typeof(Image));
            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);

            var outline = inputObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var textObj = new GameObject("Text", typeof(Text));
            textObj.transform.SetParent(inputObj.transform, false);
            var textComponent = textObj.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new(0, 0);
            textRect.anchorMax = new(1, 1);
            textRect.offsetMin = new(8, 0);
            textRect.offsetMax = new(-8, 0);

            var placeholderObj = new GameObject("Placeholder", typeof(Text));
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderComponent = placeholderObj.GetComponent<Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderComponent.color = new(1, 1, 1, 0.4f);
            placeholderComponent.alignment = TextAnchor.MiddleLeft;
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new(0, 0);
            placeholderRect.anchorMax = new(1, 1);
            placeholderRect.offsetMin = new(8, 0);
            placeholderRect.offsetMax = new(-8, 0);

            var inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;

            inputObj.transform.SetParent(parent, false);
            return inputField;
        }

        public static Toggle CreateToggle(string name, Transform parent, bool isOn = false,
            UnityAction<bool>? onValueChanged = null)
        {
            var toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);
            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = isOn;
            if (onValueChanged != null) toggle.onValueChanged.AddListener(onValueChanged);

            var toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = Vector2.zero;
            toggleRect.anchorMax = Vector2.zero;
            toggleRect.pivot = new(0.5f, 0.5f);
            toggleRect.sizeDelta = new(20, 20);
            toggleRect.anchoredPosition = Vector2.zero;

            var toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new(0.2f, 0.2f, 0.2f, 1);

            var checkmark = CreateImage("Checkmark", toggleObj.transform, new(0.2f, 0.8f, 0.2f, 1));
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new(0.2f, 0.2f);
            checkmarkRect.anchorMax = new(0.8f, 0.8f);
            checkmarkRect.sizeDelta = Vector2.zero;
            toggle.graphic = checkmark.GetComponent<Image>();

            return toggle;
        }

        public static ScrollRect CreateScrollView(string name, Transform parent, out GameObject content,
            bool vertical = true, bool horizontal = false)
        {
            var scrollView = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollView.transform.SetParent(parent, false);

            var scrollImage = scrollView.GetComponent<Image>();
            scrollImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            var mask = scrollView.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = vertical;
            scrollRect.scrollSensitivity = 1;

            content = new("Content", typeof(RectTransform));
            content.transform.SetParent(scrollView.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 1);
            contentRect.anchorMax = new(1, 1);
            contentRect.pivot = new(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            scrollRect.content = contentRect;

            return scrollRect;
        }

        public static void SetupRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta,
            Vector2? pivot = null, Vector2? anchoredPosition = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            if (pivot.HasValue) rect.pivot = pivot.Value;
            if (anchoredPosition.HasValue) rect.anchoredPosition = anchoredPosition.Value;
        }

        public static void SetupButtonColors(Button button, Color? normalColor = null, Color? highlightedColor = null,
            Color? pressedColor = null, Color? selectedColor = null)
        {
            var colors = button.colors;
            colors.normalColor = normalColor ?? new(1, 1, 1, 1);
            colors.highlightedColor = highlightedColor ?? new(0.5f, 0.7f, 0.9f, 1);
            colors.pressedColor = pressedColor ?? new(0.4f, 0.6f, 0.8f, 1);
            colors.selectedColor = selectedColor ?? new(0.5f, 0.7f, 0.9f, 1);
            button.colors = colors;
        }
    }
}