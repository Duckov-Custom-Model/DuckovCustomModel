using System;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Components;
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
            textComponent.raycastTarget = false;
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
            textComponent.overflowMode = TextOverflowModes.Ellipsis;
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

            obj.GetComponent<Image>().color = backgroundColor ?? UITheme.Control;

            var button = obj.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new(1.15f, 1.15f, 1.15f, 1);
            colors.pressedColor = new(.8f, .8f, .8f, 1);
            colors.selectedColor = Color.white;
            colors.fadeDuration = .08f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            return obj;
        }

        public static TMP_InputField CreateInputField(string name, Transform parent, string placeholder = "")
        {
            var inputObj = new GameObject(name, typeof(Image));
            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = UITheme.Border;

            var background = CreateImage("Background", inputObj.transform, UITheme.Input);
            SetupRectTransform(background, Vector2.zero, Vector2.one,
                offsetMin: new(1, 1), offsetMax: new(-1, -1));
            background.GetComponent<Image>().raycastTarget = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(inputObj.transform, false);
            SetupRectTransform(viewport, Vector2.zero, Vector2.one,
                offsetMin: new(8, 2), offsetMax: new(-8, -2));
            var textObj = new GameObject("Text", typeof(TextMeshProUGUI));
            textObj.transform.SetParent(viewport.transform, false);
            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            textComponent.color = Color.white;
            textComponent.fontSize = 14;
            textComponent.enableAutoSizing = false;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(textObj, new(0, 0), new(1, 1),
                offsetMin: new(0, 2), offsetMax: new(0, -2));

            var placeholderObj = new GameObject("Placeholder", typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(viewport.transform, false);
            var placeholderComponent = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholderComponent.text = placeholder;
            placeholderComponent.color = new(1, 1, 1, 0.4f);
            placeholderComponent.fontSize = 14;
            placeholderComponent.enableAutoSizing = false;
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(placeholderObj, new(0, 0), new(1, 1),
                offsetMin: new(0, 2), offsetMax: new(0, -2));

            var inputField = inputObj.AddComponent<TMP_InputField>();
            textComponent.raycastTarget = false;
            textComponent.enableWordWrapping = false;
            placeholderComponent.raycastTarget = false;
            placeholderComponent.enableWordWrapping = false;
            placeholderComponent.overflowMode = TextOverflowModes.Ellipsis;
            inputField.textViewport = viewport.GetComponent<RectTransform>();
            inputField.targetGraphic = background.GetComponent<Image>();
            inputField.selectionColor = new(.22f, .39f, .53f, .65f);
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;

            inputObj.transform.SetParent(parent, false);
            return inputField;
        }

        public static Toggle CreateToggle(string name, Transform parent, bool isOn = false,
            UnityAction<bool>? onValueChanged = null, bool themed = false)
        {
            var toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);
            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = isOn;
            if (onValueChanged != null) toggle.onValueChanged.AddListener(onValueChanged);

            SetupRectTransform(toggleObj, Vector2.zero, Vector2.zero,
                new(20, 20), pivot: new(0.5f, 0.5f));

            var toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = themed ? UITheme.Control : new(0.2f, 0.2f, 0.2f, 1);
            if (themed) toggle.targetGraphic = toggleImage;

            var checkmark = CreateImage("Checkmark", toggleObj.transform,
                themed ? UITheme.Highlight : new(0.2f, 0.8f, 0.2f, 1));
            SetupRectTransform(checkmark, new(0.2f, 0.2f), new(0.8f, 0.8f), Vector2.zero);
            toggle.graphic = checkmark.GetComponent<Image>();
            if (themed) toggle.graphic.raycastTarget = false;

            return toggle;
        }

        public static ScrollRect CreateScrollView(string name, Transform parent, out GameObject content,
            bool vertical = true, bool horizontal = false)
        {
            var scrollView = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollView.transform.SetParent(parent, false);

            var scrollImage = scrollView.GetComponent<Image>();
            scrollImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            scrollView.AddComponent<RectMask2D>();

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = vertical;
            scrollRect.scrollSensitivity = 1;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = scrollView.GetComponent<RectTransform>();

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

            scrollView.AddComponent<RectMask2D>();

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

        public static void SetupBoundedLeftLabel(GameObject obj, float reservedRight, float height = 36f)
        {
            SetupRectTransform(obj, new(0, .5f), new(1, .5f),
                offsetMin: new(20, -height / 2), offsetMax: new(-reservedRight, height / 2));
            var text = obj.GetComponent<TextMeshProUGUI>();
            if (text == null) return;
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = 18;
            text.fontSize = 18;
            text.overflowMode = TextOverflowModes.Ellipsis;
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
            dropdownImage.color = UITheme.Input;

            var outline = dropdownObj.AddComponent<Outline>();
            outline.effectColor = UITheme.Border;
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

            labelText.raycastTarget = false;
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            var arrowObj = CreateText("Arrow", dropdownObj.transform, "▾", 18, UITheme.MutedText,
                TextAnchor.MiddleCenter);
            SetupRectTransform(arrowObj, new(1, .5f), new(1, .5f),
                new(20, 24), pivot: new(1, .5f), anchoredPosition: new(-5, 0));

            var templateObj = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            SetupRectTransform(templateObj, new(0, 0), new(1, 0),
                new(0, 200), pivot: new(0.5f, 1), anchoredPosition: new(0, 2));

            var templateImage = templateObj.GetComponent<Image>();
            templateImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);

            var templateOutline = templateObj.AddComponent<Outline>();
            templateOutline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            templateOutline.effectDistance = new(1, -1);

            var templateScrollRect = templateObj.GetComponent<ScrollRect>();
            templateScrollRect.horizontal = false;
            templateScrollRect.vertical = true;
            templateScrollRect.scrollSensitivity = 1;
            templateScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObj.transform.SetParent(templateObj.transform, false);
            SetupRectTransform(viewportObj, Vector2.zero, Vector2.one, Vector2.zero);
            var viewportImage = viewportObj.GetComponent<Image>();
            viewportImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);
            viewportImage.raycastTarget = false;
            templateScrollRect.viewport = viewportObj.GetComponent<RectTransform>();

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(ToggleGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            SetupRectTransform(contentObj, new(0, 1), new(1, 1),
                new(0, 40), pivot: new(0.5f, 1));
            templateScrollRect.content = contentObj.GetComponent<RectTransform>();

            var itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemObj.transform.SetParent(contentObj.transform, false);
            SetupRectTransform(itemObj, new(0, 1), new(1, 1),
                new(0, 40), pivot: new(0.5f, 1));

            var itemBackgroundObj = new GameObject("Item Background", typeof(Image));
            itemBackgroundObj.transform.SetParent(itemObj.transform, false);
            SetupRectTransform(itemBackgroundObj, Vector2.zero, Vector2.one, Vector2.zero);
            var itemBackgroundImage = itemBackgroundObj.GetComponent<Image>();
            itemBackgroundImage.color = Color.white;

            var itemToggle = itemObj.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.transition = Selectable.Transition.ColorTint;

            var colors = itemToggle.colors;
            colors.normalColor = new(0.1f, 0.12f, 0.15f, 0.98f);
            colors.highlightedColor = new(0.2f, 0.25f, 0.3f, 0.98f);
            colors.pressedColor = new(0.15f, 0.18f, 0.22f, 0.98f);
            colors.selectedColor = new(0.25f, 0.3f, 0.35f, 0.98f);
            colors.disabledColor = new(0.05f, 0.06f, 0.08f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            itemToggle.colors = colors;

            var itemLabelObj = new GameObject("Item Label", typeof(TextMeshProUGUI));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelText = itemLabelObj.GetComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 15;
            itemLabelText.raycastTarget = false;
            itemLabelText.enableWordWrapping = false;
            itemLabelText.overflowMode = TextOverflowModes.Ellipsis;
            itemLabelText.color = new(0.95f, 0.95f, 0.95f, 1f);
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            SetupRectTransform(itemLabelObj, new(0, 0), new(1, 1),
                offsetMin: new(12, 4), offsetMax: new(-12, -4));

            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.itemText = itemLabelText;

            var scrollbar = CreateScrollbar(templateScrollRect, 6f, true);
            scrollbar.transform.SetParent(templateObj.transform, false);

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
            var sliderObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);

            sliderObj.GetComponent<Image>().color = Color.clear;
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;
            if (onValueChanged != null) slider.onValueChanged.AddListener(onValueChanged);

            var backgroundObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObj.transform.SetParent(sliderObj.transform, false);
            SetupRectTransform(backgroundObj, new(0, .5f), new(1, .5f), new(0, 4));
            var backgroundImage = backgroundObj.GetComponent<Image>();
            backgroundImage.color = new(0.1f, 0.1f, 0.1f, 0.5f);
            slider.targetGraphic = backgroundImage;

            var fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            SetupRectTransform(fillAreaObj, new(0, .5f), new(1, .5f), new(0, 4));

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            SetupRectTransform(fillObj, Vector2.zero, Vector2.one, Vector2.zero);
            var fillImage = fillObj.GetComponent<Image>();
            fillImage.color = UITheme.Highlight;
            fillImage.raycastTarget = false;
            slider.fillRect = fillObj.GetComponent<RectTransform>();

            var handleSlideAreaObj = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideAreaObj.transform.SetParent(sliderObj.transform, false);
            SetupRectTransform(handleSlideAreaObj, new(0, .5f), new(1, .5f), new(-14, 20));

            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(handleSlideAreaObj.transform, false);
            SetupRectTransform(handleObj, new(0.5f, 0.5f), new(0.5f, 0.5f), new(14, 0), pivot: new(0.5f, 0.5f));
            var handleImage = handleObj.GetComponent<Image>();
            handleImage.color = new(0.8f, 0.8f, 0.8f, 1f);
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;

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
                SetupRectTransform(scrollbarObj, new(1, 0), new(1, 1),
                    new Vector2(width, 0), pivot: new Vector2(1, 0.5f));
            else
                SetupRectTransform(scrollbarObj, new(0, 0), new(0, 1),
                    new Vector2(width, 0), pivot: new Vector2(0, 0.5f));

            var scrollbarImage = scrollbarObj.GetComponent<Image>();
            scrollbarImage.color = new(0.2f, 0.2f, 0.2f, 0.8f);

            var scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollRect.verticalScrollbar = scrollbar;

            var scrollbarBackground = new GameObject("Background", typeof(RectTransform), typeof(Image));
            scrollbarBackground.transform.SetParent(scrollbarObj.transform, false);
            SetupRectTransform(scrollbarBackground, Vector2.zero, Vector2.one, Vector2.zero);
            var backgroundImage = scrollbarBackground.GetComponent<Image>();
            backgroundImage.color = new(0.1f, 0.1f, 0.1f, 0.5f);
            scrollbar.targetGraphic = backgroundImage;

            var scrollbarHandle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            scrollbarHandle.transform.SetParent(scrollbarBackground.transform, false);
            SetupRectTransform(scrollbarHandle, new(0.2f, 0), new(0.8f, 1), Vector2.zero);
            var handleImage = scrollbarHandle.GetComponent<Image>();
            handleImage.color = new(0.5f, 0.5f, 0.5f, 0.8f);
            scrollbar.handleRect = scrollbarHandle.GetComponent<RectTransform>();

            return scrollbar;
        }

        public static SliderWithInputToggle CreateSliderWithInputToggle(
            string name,
            Transform parent,
            float minValue = 0f,
            float maxValue = 1f,
            float value = 1f,
            string valueFormat = "F3",
            UnityAction<float>? onValueChanged = null)
        {
            var containerObj = new GameObject(name, typeof(RectTransform));
            containerObj.transform.SetParent(parent, false);
            var component = containerObj.AddComponent<SliderWithInputToggle>();
            var slider = CreateSlider("Slider", containerObj.transform, minValue, maxValue, value);
            SetupRectTransform(slider.gameObject, new(0, .5f), new(1, .5f),
                offsetMin: new(0, -15), offsetMax: new(-84, 15));
            var inputField = CreateInputField("Value", containerObj.transform);
            SetupRectTransform(inputField.gameObject, new(1, .5f), new(1, .5f), new(76, 30),
                pivot: new(1, .5f), anchoredPosition: Vector2.zero);
            inputField.textComponent.alignment = TextAlignmentOptions.Center;
            component.Initialize(slider, inputField, minValue, maxValue, value, valueFormat, onValueChanged);
            return component;
        }
    }
}
