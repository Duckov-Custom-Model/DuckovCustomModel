using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public partial class TargetSettingsPanel
    {
        private GameObject? _loadingOverlay;
        private CanvasGroup? _settingsGroup;
        private bool _settingsLoading;

        private void OnModelLoadingChanged(ModelHandler handler)
        {
            UpdateLoadingOverlay();
            if (!handler.IsModelLoading && _currentTarget != null &&
                handler.TargetTypeId == _currentTarget.GetTargetTypeId())
                UpdateRuntimeSettingsVisibility();
        }

        private void UpdateLoadingOverlay()
        {
            if (_content == null || _scrollView == null) return;
            if (_settingsGroup == null)
            {
                _settingsGroup = _content.GetComponent<CanvasGroup>();
                if (_settingsGroup == null) _settingsGroup = _content.AddComponent<CanvasGroup>();
            }

            var loading = false;
            if (_currentTarget != null)
            {
                var target = _currentTarget.GetTargetTypeId();
                var model = _currentTarget.HasFallbackModel
                    ? _currentTarget.UsingFallbackModel
                    : _currentTarget.UsingModel;
                foreach (var handler in ModelManager.GetAllHandlers())
                    if (handler.IsModelLoading && handler.LoadingModelId == model &&
                        (handler.TargetTypeId == target || target == ModelTargetType.AllAICharacters &&
                            ModelTargetType.IsAICharacterTargetType(handler.TargetTypeId)))
                    {
                        loading = true;
                        break;
                    }
            }

            _settingsLoading = loading;
            if (loading && _loadingOverlay == null)
            {
                _loadingOverlay = UIFactory.CreateImage("ModelLoadingOverlay", _scrollView.transform,
                    new Color(.07f, .09f, .12f, .92f));
                _loadingOverlay.AddComponent<LayoutElement>().ignoreLayout = true;
                UIFactory.SetupRectTransform(_loadingOverlay, Vector2.zero, Vector2.one, Vector2.zero);
                var label = UIFactory.CreateText("Loading", _loadingOverlay.transform, Localization.Loading, 22,
                    UITheme.Text, TextAnchor.MiddleCenter);
                UIFactory.SetupRectTransform(label, Vector2.zero, Vector2.one, Vector2.zero);
                UIFactory.SetLocalizedText(label, () => Localization.Loading);
            }

            if (loading && _settingsGroup.interactable && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject is { } selected &&
                selected.transform.IsChildOf(_content.transform))
                EventSystem.current.SetSelectedGameObject(null);
            _settingsGroup.interactable = !loading;
            _settingsGroup.blocksRaycasts = !loading;
            if (_loadingOverlay != null)
            {
                _loadingOverlay.SetActive(loading);
                if (loading) _loadingOverlay.transform.SetAsLastSibling();
            }
        }
    }
}
