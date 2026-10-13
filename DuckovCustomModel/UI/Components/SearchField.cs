using System;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using TMPro;
using UnityEngine;

namespace DuckovCustomModel.UI.Components
{
    public class SearchField : MonoBehaviour
    {
        private string _appliedSearch = string.Empty;
        private float _applyAt;
        private TMP_InputField? _inputField;
        private bool _pending;
        private string _searchText = string.Empty;

        private void Update()
        {
            if (_pending && Time.unscaledTime >= _applyAt) ApplySearch();
        }

        private void OnDisable()
        {
            if (_pending) ApplySearch();
        }

        public event Action<string>? OnSearchChanged;

        public void Initialize(Transform parent)
        {
            _inputField = UIFactory.CreateInputField("SearchField", parent, Localization.SearchPlaceholder);
            UIFactory.SetupRectTransform(_inputField.gameObject, Vector2.zero, Vector2.one, Vector2.zero);
            _inputField.onValueChanged.AddListener(OnSearchValueChanged);

            if (_inputField == null || _inputField.placeholder == null) return;
            var placeholderText = _inputField.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText == null) return;
            UIFactory.SetLocalizedText(_inputField.placeholder.gameObject,
                () => Localization.SearchPlaceholder);
        }

        private void ApplySearch()
        {
            _pending = false;
            if (_appliedSearch == _searchText) return;
            _appliedSearch = _searchText;
            OnSearchChanged?.Invoke(_searchText);
        }

        private void OnSearchValueChanged(string text)
        {
            _searchText = text;
            _applyAt = Time.unscaledTime + .15f;
            _pending = true;
        }
    }
}
