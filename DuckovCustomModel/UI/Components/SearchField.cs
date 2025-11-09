using System;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class SearchField : MonoBehaviour
    {
        private InputField? _inputField;
        private Text? _placeholderText;
        private string _searchText = string.Empty;

        private void OnDestroy()
        {
            Localization.OnLanguageChangedEvent -= OnLanguageChanged;
        }

        public event Action<string>? OnSearchChanged;

        public void Initialize(Transform parent)
        {
            _inputField = UIFactory.CreateInputField("SearchField", parent, Localization.SearchPlaceholder);
            UIFactory.SetupRectTransform(_inputField.gameObject, Vector2.zero, Vector2.one, Vector2.zero);
            _inputField.onValueChanged.AddListener(OnSearchValueChanged);

            if (_inputField != null && _inputField.placeholder != null)
            {
                _placeholderText = _inputField.placeholder.GetComponent<Text>();
                if (_placeholderText != null)
                    _placeholderText.fontSize += 4;
            }

            Localization.OnLanguageChangedEvent += OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            if (_placeholderText != null)
                _placeholderText.text = Localization.SearchPlaceholder;
        }

        private void OnSearchValueChanged(string text)
        {
            _searchText = text;
            OnSearchChanged?.Invoke(_searchText);
        }
    }
}