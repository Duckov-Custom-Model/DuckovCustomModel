using System;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class FunctionButtonBar : MonoBehaviour
    {
        private bool _isRefreshing;
        private Button? _refreshButton;
        private Text? _refreshButtonText;
        private Button? _resetInvalidModelsButton;
        private Text? _resetInvalidModelsButtonText;

        private void OnDestroy()
        {
            ModelListManager.OnRefreshStarted -= OnModelListRefreshStarted;
            ModelListManager.OnRefreshCompleted -= OnModelListRefreshCompleted;
            Localization.OnLanguageChangedEvent -= OnLanguageChanged;
        }

        public event Action? OnRefresh;
        public event Action? OnResetInvalidModels;

        public void Initialize(Transform parent)
        {
            var buttonBar = UIFactory.CreateImage("FunctionButtonBar", parent, new(0.15f, 0.18f, 0.22f, 0.9f));
            UIFactory.SetupRectTransform(buttonBar, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutGroup = buttonBar.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            BuildRefreshButton(buttonBar);
            BuildResetInvalidModelsButton(buttonBar);

            ModelListManager.OnRefreshStarted += OnModelListRefreshStarted;
            ModelListManager.OnRefreshCompleted += OnModelListRefreshCompleted;
            Localization.OnLanguageChangedEvent += OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            if (_refreshButtonText != null)
                _refreshButtonText.text = _isRefreshing ? Localization.Loading : Localization.Refresh;

            if (_resetInvalidModelsButtonText != null)
                _resetInvalidModelsButtonText.text = Localization.ResetInvalidModels;
        }

        private void BuildRefreshButton(GameObject parent)
        {
            _refreshButton = UIFactory.CreateButton("RefreshButton", parent.transform, OnRefreshButtonClicked,
                new(0.2f, 0.3f, 0.4f, 1)).GetComponent<Button>();
            var refreshButtonRect = _refreshButton.GetComponent<RectTransform>();
            refreshButtonRect.sizeDelta = new(100, 30);

            _refreshButtonText = UIFactory.CreateText("Text", _refreshButton.transform, Localization.Refresh, 18,
                Color.white, TextAnchor.MiddleCenter).GetComponent<Text>();
            UIFactory.SetupRectTransform(_refreshButtonText.gameObject, Vector2.zero, Vector2.one, Vector2.zero);

            UIFactory.SetupButtonColors(_refreshButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1),
                new(0.3f, 0.4f, 0.5f, 1), new(0.4f, 0.5f, 0.6f, 1));
        }

        private void BuildResetInvalidModelsButton(GameObject parent)
        {
            _resetInvalidModelsButton = UIFactory.CreateButton("ResetInvalidModelsButton", parent.transform,
                OnResetInvalidModelsButtonClicked, new(0.4f, 0.2f, 0.2f, 1)).GetComponent<Button>();
            var resetButtonRect = _resetInvalidModelsButton.GetComponent<RectTransform>();
            resetButtonRect.sizeDelta = new(150, 30);

            _resetInvalidModelsButtonText = UIFactory.CreateText("Text", _resetInvalidModelsButton.transform,
                Localization.ResetInvalidModels, 18, Color.white, TextAnchor.MiddleCenter).GetComponent<Text>();
            UIFactory.SetupRectTransform(_resetInvalidModelsButtonText.gameObject, Vector2.zero, Vector2.one,
                Vector2.zero);

            UIFactory.SetupButtonColors(_resetInvalidModelsButton, new(1, 1, 1, 1), new(0.6f, 0.4f, 0.4f, 1),
                new(0.5f, 0.3f, 0.3f, 1), new(0.6f, 0.4f, 0.4f, 1));
        }

        private void OnRefreshButtonClicked()
        {
            if (_isRefreshing) return;
            OnRefresh?.Invoke();
        }

        private void OnResetInvalidModelsButtonClicked()
        {
            OnResetInvalidModels?.Invoke();
        }

        private void OnModelListRefreshStarted()
        {
            _isRefreshing = true;
            UpdateRefreshButtonState(true);
        }

        private void OnModelListRefreshCompleted()
        {
            _isRefreshing = false;
            UpdateRefreshButtonState(false);
        }

        private void UpdateRefreshButtonState(bool isLoading)
        {
            if (_refreshButton != null) _refreshButton.interactable = !isLoading;

            if (_refreshButtonText != null)
                _refreshButtonText.text = isLoading ? Localization.Loading : Localization.Refresh;
        }
    }
}