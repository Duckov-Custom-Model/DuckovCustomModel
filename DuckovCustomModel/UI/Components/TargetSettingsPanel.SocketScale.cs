using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public partial class TargetSettingsPanel
    {
        private readonly Dictionary<(string Target, string Model, string Slot), bool> _socketScaleExpanded = [];
        private readonly Dictionary<(string Target, string Model), bool> _socketScaleSectionExpanded = [];
        private readonly Dictionary<string, SliderWithInputToggle> _socketScaleSliders = new(StringComparer.Ordinal);
        private float _nextSocketScaleRefresh;
        private string _socketScaleModel = string.Empty;
        private GameObject? _socketScaleSection;
        private string _socketScaleTarget = string.Empty;

        private void Update()
        {
            if (Time.unscaledTime < _nextSocketScaleRefresh) return;
            _nextSocketScaleRefresh = Time.unscaledTime + 0.5f;
            RefreshSocketScaleSettings();
        }

        private void OnEnable()
        {
            ModelSocketScaleManager.OnScaleChanged += OnSocketScaleChanged;
            _nextSocketScaleRefresh = 0;
            OnSocketScaleChanged(_socketScaleTarget, _socketScaleModel);
        }

        private void OnDisable()
        {
            ModelSocketScaleManager.OnScaleChanged -= OnSocketScaleChanged;
        }

        private void BuildSocketScaleSection()
        {
            if (_content == null) return;
            _socketScaleSection = CreateSocketScaleColumn("SocketScales", _content.transform, 6, 0);
            _socketScaleSliders.Clear();
            _socketScaleTarget = _socketScaleModel = string.Empty;
            _socketScaleSection.SetActive(false);
        }

        private void RefreshSocketScaleSettings()
        {
            if (_socketScaleSection == null || _currentTarget == null) return;
            var target = _currentTarget.GetTargetTypeId();
            var model = ModEntry.UsingModel?.GetModelID(target);
            if (string.IsNullOrWhiteSpace(model) && _currentTarget.IsAICharacter())
                model = ModEntry.UsingModel?.GetModelID(ModelTargetType.AllAICharacters);
            if (target.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(model))
            {
                _socketScaleSection.SetActive(false);
                return;
            }

            var slots = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var handler in ModelManager.GetAllModelHandlersByTargetType(target))
                if (handler != null && handler.CurrentModelInfo?.ModelID == model)
                    foreach (var slot in handler.GetAdjustableSocketNames())
                        slots.Add(slot);
            _socketScaleSection.SetActive(slots.Count > 0);
            if (slots.Count == 0) return;
            if (_socketScaleTarget == target && _socketScaleModel == model &&
                slots.SetEquals(_socketScaleSliders.Keys)) return;

            foreach (Transform child in _socketScaleSection.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            _socketScaleSliders.Clear();
            _socketScaleTarget = target;
            _socketScaleModel = model;
            var sectionKey = (target, model);
            _socketScaleSectionExpanded.TryGetValue(sectionKey, out var sectionExpanded);
            var sectionHeader = UIFactory.CreateButton("SocketScaleHeader", _socketScaleSection.transform,
                backgroundColor: new Color(0.12f, 0.14f, 0.16f, 0.9f));
            SetSocketScaleRowSize(sectionHeader, 34);
            var heading = UIFactory.CreateText("SocketScaleHeading", sectionHeader.transform,
                (sectionExpanded ? "▼ " : "▶ ") + Localization.SocketScales, 16, Color.white);
            UIFactory.SetupButtonText(heading);
            var headingLabel = heading.GetComponent<TMP_Text>();
            headingLabel.enableWordWrapping = false;
            headingLabel.overflowMode = TextOverflowModes.Ellipsis;
            UIFactory.SetLocalizedText(heading,
                () => (sectionExpanded ? "▼ " : "▶ ") + Localization.SocketScales);
            var sectionBody = CreateSocketScaleColumn("SocketScaleItems", _socketScaleSection.transform, 6, 0);
            sectionBody.SetActive(sectionExpanded);
            sectionHeader.GetComponent<Button>().onClick.AddListener(() =>
            {
                sectionExpanded = !sectionExpanded;
                _socketScaleSectionExpanded[sectionKey] = sectionExpanded;
                sectionBody.SetActive(sectionExpanded);
                headingLabel.text = (sectionExpanded ? "▼ " : "▶ ") + Localization.SocketScales;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_content!.transform);
            });
            var description = UIFactory.CreateText("ScaleDescription", sectionBody.transform,
                Localization.SocketScaleMultiplier, 13, new Color(0.75f, 0.78f, 0.8f));
            SetSocketScaleRowSize(description, 40);
            UIFactory.SetLocalizedText(description, () => Localization.SocketScaleMultiplier);
            var resetAll = UIFactory.CreateButton("ResetAllSocketScales", sectionBody.transform,
                () => ModelSocketScaleManager.ResetAll(target, model), new Color(0.3f, 0.35f, 0.4f, 1));
            SetSocketScaleRowSize(resetAll, 34);
            var resetAllText = UIFactory.CreateText("Text", resetAll.transform,
                Localization.ResetAllSocketScales, 14, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(resetAllText);
            UIFactory.SetLocalizedText(resetAllText, () => Localization.ResetAllSocketScales);
            foreach (var slot in slots) BuildSocketScaleFoldout(sectionBody.transform, target, model, slot);
        }

        private void BuildSocketScaleFoldout(Transform parent, string target, string model, string slot)
        {
            var group = CreateSocketScaleColumn("SocketScale_" + slot, parent, 0, 0);
            var stateKey = (target, model, slot);
            _socketScaleExpanded.TryGetValue(stateKey, out var expanded);
            var header = UIFactory.CreateButton("Header", group.transform,
                backgroundColor: new Color(0.12f, 0.14f, 0.16f, 0.9f));
            SetSocketScaleRowSize(header, 30);
            var headerText = UIFactory.CreateText("Text", header.transform,
                (expanded ? "▼ " : "▶ ") + Localization.GetSocketName(slot), 16, Color.white);
            UIFactory.SetupButtonText(headerText, 12, 16);
            var headerLabel = headerText.GetComponent<TMP_Text>();
            headerLabel.richText = false;
            headerLabel.enableWordWrapping = false;
            headerLabel.overflowMode = TextOverflowModes.Ellipsis;
            UIFactory.SetLocalizedText(headerText,
                () => (expanded ? "▼ " : "▶ ") + Localization.GetSocketName(slot));
            var body = CreateSocketScaleColumn("Controls", group.transform, 4, 6);
            body.SetActive(expanded);
            header.GetComponent<Button>().onClick.AddListener(() =>
            {
                expanded = !expanded;
                _socketScaleExpanded[stateKey] = expanded;
                body.SetActive(expanded);
                headerLabel.text = (expanded ? "▼ " : "▶ ") + Localization.GetSocketName(slot);
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_content!.transform);
            });
            var slider = UIFactory.CreateSliderWithInputToggle("Scale", body.transform,
                ModelSocketScaleManager.MinScale, ModelSocketScaleManager.MaxScale,
                ModelSocketScaleManager.GetScale(target, model, slot), "F2",
                value => ModelSocketScaleManager.SetScale(target, model, slot, value));
            SetSocketScaleRowSize(slider.gameObject, 32);
            _socketScaleSliders.Add(slot, slider);
            var reset = UIFactory.CreateButton("Reset", body.transform,
                () => ModelSocketScaleManager.ResetScale(target, model, slot), new Color(0.3f, 0.35f, 0.4f, 1));
            SetSocketScaleRowSize(reset, 30);
            var resetText = UIFactory.CreateText("Text", reset.transform,
                Localization.Reset, 14, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(resetText);
            UIFactory.SetLocalizedText(resetText, () => Localization.Reset);
        }

        private void OnSocketScaleChanged(string target, string model)
        {
            if (_socketScaleTarget != target || _socketScaleModel != model) return;
            if (_socketScaleSliders.Count == 0) return;
            var scales = ModelSocketScaleManager.GetScales(target, model);
            foreach (var pair in _socketScaleSliders)
                if (pair.Value != null)
                    pair.Value.SetValueWithoutNotify(scales.TryGetValue(pair.Key, out var scale) ? scale : 1);
        }

        private static GameObject CreateSocketScaleColumn(string name, Transform parent, float spacing, int padding)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            column.transform.SetParent(parent, false);
            UIFactory.SetupVerticalLayoutGroup(column, spacing, new(padding, padding, padding, padding),
                TextAnchor.UpperLeft, true, true, true);
            var layout = column.GetComponent<LayoutElement>();
            layout.minWidth = 0;
            layout.preferredWidth = 0;
            layout.flexibleWidth = 1;
            layout.flexibleHeight = 0;
            return column;
        }

        private static void SetSocketScaleRowSize(GameObject row, float height)
        {
            var layout = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
            layout.minWidth = 0;
            layout.preferredWidth = 0;
            layout.flexibleWidth = 1;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0;
        }
    }
}
