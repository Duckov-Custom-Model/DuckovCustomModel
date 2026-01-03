using System;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Utils;
using TMPro;
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
            var obj = new GameObject(name, typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var textComponent = obj.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = color ?? Color.white;
            textComponent.alignment = ConvertTextAnchor(alignment);
            textComponent.fontStyle = ConvertFontStyle(fontStyle);
            return obj;
        }

        public static GameObject CreateLocalizedText(string name, Transform parent, Func<string> textGetter,
            int fontSize = 14, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft,
            FontStyle fontStyle = FontStyle.Normal)
        {
            var obj = CreateText(name, parent, textGetter(), fontSize, color, alignment, fontStyle);
            SetLocalizedText(obj, textGetter);
            return obj;
        }

        public static void SetLocalizedText(GameObject textObj, Func<string>? textGetter)
        {
            if (textObj == null || textGetter == null) return;

            var localizedText = textObj.GetComponent<LocalizedText>();
            if (localizedText == null)
                localizedText = textObj.AddComponent<LocalizedText>();

            localizedText.SetTextGetter(textGetter);
        }

        public static void SetupButtonText(GameObject textObj, int minFontSize = 12, int maxFontSize = 18,
            float padding = 8f)
        {
            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            if (textComponent == null) return;

            textComponent.enableWordWrapping = true;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = minFontSize;
            textComponent.fontSizeMax = maxFontSize;

            SetupRectTransform(textObj, new(0, 0), new(1, 1),
                offsetMin: new(padding, 0), offsetMax: new(-padding, 0));
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

        public static TMP_InputField CreateInputField(string name, Transform parent, string placeholder = "")
        {
            var inputObj = new GameObject(name, typeof(Image));
            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);

            var outline = inputObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var textObj = new GameObject("Text", typeof(TextMeshProUGUI));
            textObj.transform.SetParent(inputObj.transform, false);
            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            textComponent.color = Color.white;
            textComponent.fontSize = 14;
            textComponent.enableAutoSizing = false;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(textObj, new(0, 0), new(1, 1),
                offsetMin: new(8, 4), offsetMax: new(-8, -4));

            var placeholderObj = new GameObject("Placeholder", typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderComponent = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholderComponent.text = placeholder;
            placeholderComponent.color = new(1, 1, 1, 0.4f);
            placeholderComponent.fontSize = 14;
            placeholderComponent.enableAutoSizing = false;
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(placeholderObj, new(0, 0), new(1, 1),
                offsetMin: new(8, 4), offsetMax: new(-8, -4));

            var inputField = inputObj.AddComponent<TMP_InputField>();
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

            SetupRectTransform(toggleObj, Vector2.zero, Vector2.zero,
                new(20, 20), pivot: new(0.5f, 0.5f));

            var toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new(0.2f, 0.2f, 0.2f, 1);

            var checkmark = CreateImage("Checkmark", toggleObj.transform, new(0.2f, 0.8f, 0.2f, 1));
            SetupRectTransform(checkmark, new(0.2f, 0.2f), new(0.8f, 0.8f), Vector2.zero);
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
            SetupRectTransform(content, new(0, 1), new(1, 1), Vector2.zero, pivot: new(0, 1),
                anchoredPosition: Vector2.zero);

            scrollRect.content = contentRect;

            return scrollRect;
        }

        public static ScrollRect CreateNonInteractiveScrollView(string name, Transform parent, out GameObject content,
            bool vertical = true, bool horizontal = false)
        {
            var scrollView = new GameObject(name, typeof(RectTransform), typeof(NonInteractiveScrollRect),
                typeof(Image));
            scrollView.transform.SetParent(parent, false);

            var scrollImage = scrollView.GetComponent<Image>();
            scrollImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);
            scrollImage.raycastTarget = false;

            var mask = scrollView.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scrollRect = scrollView.GetComponent<NonInteractiveScrollRect>();
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = vertical;
            scrollRect.scrollSensitivity = 0;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            content = new("Content", typeof(RectTransform));
            content.transform.SetParent(scrollView.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            SetupRectTransform(content, new(0, 1), new(1, 1), Vector2.zero, pivot: new(0, 1),
                anchoredPosition: Vector2.zero);

            scrollRect.content = contentRect;

            return scrollRect;
        }

        public static void SetupRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax,
            Vector2? sizeDelta = null, Vector2? offsetMin = null, Vector2? offsetMax = null,
            Vector2? pivot = null, Vector2? anchoredPosition = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (offsetMin.HasValue || offsetMax.HasValue)
            {
                if (offsetMin.HasValue) rect.offsetMin = offsetMin.Value;
                if (offsetMax.HasValue) rect.offsetMax = offsetMax.Value;
            }
            else if (sizeDelta.HasValue)
            {
                rect.sizeDelta = sizeDelta.Value;
            }

            if (pivot.HasValue) rect.pivot = pivot.Value;
            if (anchoredPosition.HasValue) rect.anchoredPosition = anchoredPosition.Value;
        }

        public static void SetupAnchor(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2? pivot = null,
            Vector2? sizeDelta = null, Vector2? anchoredPosition = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (pivot.HasValue) rect.pivot = pivot.Value;
            if (sizeDelta.HasValue) rect.sizeDelta = sizeDelta.Value;
            if (anchoredPosition.HasValue) rect.anchoredPosition = anchoredPosition.Value;
        }

        public static void SetupLeftLabel(GameObject obj, float height = 30f, float leftOffset = 20f)
        {
            SetupAnchor(obj, new(0, 0.5f), new(0, 0.5f), new(0, 0.5f), new(0, height), new(leftOffset, 0));
        }

        public static void SetupRightControl(GameObject obj, Vector2 sizeDelta, float rightOffset = -20f)
        {
            SetupAnchor(obj, new(1, 0.5f), new(1, 0.5f), new(1, 0.5f), sizeDelta, new(rightOffset, 0));
        }

        public static void SetupRightLabel(GameObject obj, float height = 25f, float rightOffset = -120f)
        {
            SetupAnchor(obj, new(1, 0.5f), new(1, 0.5f), new(1, 0.5f), new(0, height), new(rightOffset, 0));
        }

        public static void SetupContentSizeFitter(GameObject obj,
            ContentSizeFitter.FitMode horizontalFit = ContentSizeFitter.FitMode.PreferredSize,
            ContentSizeFitter.FitMode verticalFit = ContentSizeFitter.FitMode.PreferredSize)
        {
            var sizeFitter = obj.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null) sizeFitter = obj.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = horizontalFit;
            sizeFitter.verticalFit = verticalFit;
        }

        public static void SetupVerticalLayoutGroup(GameObject obj, float spacing = 10f,
            RectOffset? padding = null, TextAnchor childAlignment = TextAnchor.UpperCenter,
            bool childControlWidth = true, bool childControlHeight = false,
            bool childForceExpandWidth = false, bool childForceExpandHeight = false)
        {
            var layoutGroup = obj.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = obj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = spacing;
            layoutGroup.padding = padding ?? new(10, 10, 10, 10);
            layoutGroup.childAlignment = childAlignment;
            layoutGroup.childControlWidth = childControlWidth;
            layoutGroup.childControlHeight = childControlHeight;
            layoutGroup.childForceExpandWidth = childForceExpandWidth;
            layoutGroup.childForceExpandHeight = childForceExpandHeight;
        }

        public static void SetupHorizontalLayoutGroup(GameObject obj, float spacing = 8f,
            RectOffset? padding = null, TextAnchor childAlignment = TextAnchor.MiddleLeft,
            bool childControlWidth = false, bool childControlHeight = true,
            bool childForceExpandWidth = false, bool childForceExpandHeight = true)
        {
            var layoutGroup = obj.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = obj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = spacing;
            layoutGroup.padding = padding ?? new(10, 8, 10, 8);
            layoutGroup.childAlignment = childAlignment;
            layoutGroup.childControlWidth = childControlWidth;
            layoutGroup.childControlHeight = childControlHeight;
            layoutGroup.childForceExpandWidth = childForceExpandWidth;
            layoutGroup.childForceExpandHeight = childForceExpandHeight;
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

        public static TMP_Dropdown CreateDropdown(string name, Transform parent,
            UnityAction<int>? onValueChanged = null)
        {
            var dropdownObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            dropdownObj.transform.SetParent(parent, false);

            var dropdownImage = dropdownObj.GetComponent<Image>();
            dropdownImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);

            var outline = dropdownObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var dropdown = dropdownObj.GetComponent<TMP_Dropdown>();
            if (onValueChanged != null) dropdown.onValueChanged.AddListener(onValueChanged);

            var labelObj = new GameObject("Label", typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(dropdownObj.transform, false);
            var labelText = labelObj.GetComponent<TextMeshProUGUI>();
            labelText.fontSize = 16;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(labelObj, new(0, 0), new(1, 1),
                offsetMin: new(10, 2), offsetMax: new(-25, -2));
            dropdown.captionText = labelText;

            var arrowObj = new GameObject("Arrow", typeof(Image));
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            var arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.color = Color.white;
            SetupRectTransform(arrowObj, new(1, 0.5f), new(1, 0.5f),
                new(20, 20), pivot: new(1, 0.5f), anchoredPosition: new(-5, 0));
            dropdown.captionImage = arrowImage;

            var templateObj = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            SetupRectTransform(templateObj, new(0, 0), new(1, 0),
                new(0, 150), pivot: new(0.5f, 1), anchoredPosition: new(0, 2));

            var templateImage = templateObj.GetComponent<Image>();
            templateImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);

            var templateScrollRect = templateObj.GetComponent<ScrollRect>();
            templateScrollRect.horizontal = false;
            templateScrollRect.vertical = true;

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(templateObj.transform, false);
            SetupRectTransform(viewportObj, Vector2.zero, Vector2.one, Vector2.zero);
            templateScrollRect.viewport = viewportObj.GetComponent<RectTransform>();

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(ToggleGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            SetupRectTransform(contentObj, new(0, 1), new(1, 1),
                new(0, 36), pivot: new(0.5f, 1));
            templateScrollRect.content = contentObj.GetComponent<RectTransform>();

            var itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemObj.transform.SetParent(contentObj.transform, false);
            SetupRectTransform(itemObj, new(0, 1), new(1, 1),
                new(0, 28), pivot: new(0.5f, 1));

            var itemBackgroundObj = new GameObject("Item Background", typeof(Image));
            itemBackgroundObj.transform.SetParent(itemObj.transform, false);
            SetupRectTransform(itemBackgroundObj, new(0, 0.1f), new(1, 0.9f), Vector2.zero);
            var itemBackgroundImage = itemBackgroundObj.GetComponent<Image>();
            itemBackgroundImage.color = new(0.05f, 0.06f, 0.08f, 0.95f);

            var itemToggle = itemObj.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBackgroundImage;

            var colors = itemToggle.colors;
            colors.normalColor = new(0.05f, 0.06f, 0.08f, 0.95f);
            colors.highlightedColor = new(0.15f, 0.18f, 0.22f, 0.95f);
            colors.pressedColor = new(0.1f, 0.12f, 0.15f, 0.95f);
            colors.selectedColor = new(0.15f, 0.18f, 0.22f, 0.95f);
            colors.disabledColor = new(0.05f, 0.06f, 0.08f, 0.5f);
            itemToggle.colors = colors;

            var itemLabelObj = new GameObject("Item Label", typeof(TextMeshProUGUI));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelText = itemLabelObj.GetComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 16;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(itemLabelObj, new(0, 0), new(1, 1),
                offsetMin: new(10, 2), offsetMax: new(-10, -2));

            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.itemText = itemLabelText;

            return dropdown;
        }

        private static TextAlignmentOptions ConvertTextAnchor(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Midline,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.MidlineLeft,
            };
        }

        private static FontStyles ConvertFontStyle(FontStyle style)
        {
            return style switch
            {
                FontStyle.Normal => FontStyles.Normal,
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal,
            };
        }

        public static Slider CreateSlider(string name, Transform parent, float minValue = 0f, float maxValue = 1f,
            float value = 1f, UnityAction<float>? onValueChanged = null)
        {
            var sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);

            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;
            if (onValueChanged != null) slider.onValueChanged.AddListener(onValueChanged);

            var backgroundObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObj.transform.SetParent(sliderObj.transform, false);
            SetupRectTransform(backgroundObj, new(0, 0.25f), new(1, 0.75f), Vector2.zero);
            var backgroundImage = backgroundObj.GetComponent<Image>();
            backgroundImage.color = new(0.1f, 0.1f, 0.1f, 0.5f);
            slider.targetGraphic = backgroundImage;

            var fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            SetupRectTransform(fillAreaObj, new(0, 0.25f), new(1, 0.75f), Vector2.zero);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            SetupRectTransform(fillObj, Vector2.zero, Vector2.one, Vector2.zero);
            var fillImage = fillObj.GetComponent<Image>();
            fillImage.color = new(0.2f, 0.6f, 0.9f, 1f);
            slider.fillRect = fillObj.GetComponent<RectTransform>();

            var handleSlideAreaObj = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideAreaObj.transform.SetParent(sliderObj.transform, false);
            SetupRectTransform(handleSlideAreaObj, Vector2.zero, Vector2.one, Vector2.zero);

            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(handleSlideAreaObj.transform, false);
            SetupRectTransform(handleObj, new(0.5f, 0.25f), new(0.5f, 0.75f), new(8, -10), pivot: new(0.5f, 0.5f));
            var handleImage = handleObj.GetComponent<Image>();
            handleImage.color = new(0.8f, 0.8f, 0.8f, 1f);
            slider.handleRect = handleObj.GetComponent<RectTransform>();

            return slider;
        }

        public static Scrollbar CreateScrollbar(ScrollRect scrollRect, float width = 6f)
        {
            return CreateScrollbar(scrollRect, width, true);
        }

        public static Scrollbar CreateScrollbar(ScrollRect scrollRect, float width, bool onRight)
        {
            var scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));

            if (onRight)
                SetupRectTransform(scrollbarObj, new Vector2(1, 0), new Vector2(1, 1),
                    new Vector2(width, 0), pivot: new Vector2(1, 0.5f));
            else
                SetupRectTransform(scrollbarObj, new Vector2(0, 0), new Vector2(0, 1),
                    new Vector2(width, 0), pivot: new Vector2(0, 0.5f));

            var scrollbarImage = scrollbarObj.GetComponent<Image>();
            scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollRect.verticalScrollbar = scrollbar;

            var scrollbarBackground = new GameObject("Background", typeof(RectTransform), typeof(Image));
            scrollbarBackground.transform.SetParent(scrollbarObj.transform, false);
            SetupRectTransform(scrollbarBackground, Vector2.zero, Vector2.one, Vector2.zero);
            var backgroundImage = scrollbarBackground.GetComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            scrollbar.targetGraphic = backgroundImage;

            var scrollbarHandle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            scrollbarHandle.transform.SetParent(scrollbarBackground.transform, false);
            SetupRectTransform(scrollbarHandle, new Vector2(0.2f, 0), new Vector2(0.8f, 1), Vector2.zero);
            var handleImage = scrollbarHandle.GetComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            scrollbar.handleRect = scrollbarHandle.GetComponent<RectTransform>();

            return scrollbar;
        }
    }
}
