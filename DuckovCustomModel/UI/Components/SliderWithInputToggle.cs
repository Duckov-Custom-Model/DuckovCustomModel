using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class SliderWithInputToggle : MonoBehaviour
    {
        private GameObject? _inputContainer;
        private TMP_InputField? _inputField;
        private bool _isInputMode;
        private float _maxValue;

        private float _minValue;
        private Slider? _slider;
        private GameObject? _sliderContainer;
        private Button? _toggleButton;
        private TMP_Text? _toggleButtonText;
        private string _valueFormat = "F3";
        private TMP_Text? _valueText;

        public float Value
        {
            get => _slider?.value ?? _minValue;
            set => SetValue(value, true);
        }

        public event UnityAction<float>? OnValueChanged;

        public void Initialize(
            Button toggleButton,
            TMP_Text toggleButtonText,
            GameObject sliderContainer,
            GameObject inputContainer,
            Slider slider,
            TMP_InputField inputField,
            TMP_Text valueText,
            float minValue,
            float maxValue,
            float initialValue,
            string valueFormat,
            UnityAction<float>? onValueChanged)
        {
            _toggleButton = toggleButton;
            _toggleButtonText = toggleButtonText;
            _sliderContainer = sliderContainer;
            _inputContainer = inputContainer;
            _slider = slider;
            _inputField = inputField;
            _valueText = valueText;
            _minValue = minValue;
            _maxValue = maxValue;
            _valueFormat = valueFormat;

            if (onValueChanged != null)
                OnValueChanged += onValueChanged;

            _toggleButton.onClick.AddListener(ToggleInputMode);
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
            _inputField.onEndEdit.AddListener(OnInputEndEdit);
            _inputField.contentType = TMP_InputField.ContentType.DecimalNumber;

            SetValue(initialValue, false);
            UpdateDisplay();
        }

        private void ToggleInputMode()
        {
            _isInputMode = !_isInputMode;
            UpdateDisplay();

            if (!_isInputMode || _inputField == null) return;
            _inputField.text = Value.ToString(_valueFormat, CultureInfo.InvariantCulture);
            _inputField.Select();
            _inputField.ActivateInputField();
        }

        private void UpdateDisplay()
        {
            if (_toggleButton != null)
                _toggleButton.gameObject.SetActive(!_isInputMode);

            if (_sliderContainer != null)
                _sliderContainer.SetActive(!_isInputMode);

            if (_inputContainer != null)
                _inputContainer.SetActive(_isInputMode);
        }

        private void OnSliderValueChanged(float value)
        {
            if (_valueText != null)
                _valueText.text = value.ToString(_valueFormat, CultureInfo.InvariantCulture);

            OnValueChanged?.Invoke(value);
        }

        private void OnInputEndEdit(string text)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                value = Mathf.Clamp(value, _minValue, _maxValue);
                SetValue(value, true);
            }
            else if (_inputField != null)
            {
                _inputField.text = Value.ToString(_valueFormat, CultureInfo.InvariantCulture);
            }

            _isInputMode = false;
            UpdateDisplay();
        }

        private void SetValue(float value, bool notify)
        {
            value = Mathf.Clamp(value, _minValue, _maxValue);

            if (_slider != null) _slider.SetValueWithoutNotify(value);

            if (_valueText != null)
                _valueText.text = value.ToString(_valueFormat, CultureInfo.InvariantCulture);

            if (_inputField != null)
                _inputField.SetTextWithoutNotify(value.ToString(_valueFormat, CultureInfo.InvariantCulture));

            if (notify)
                OnValueChanged?.Invoke(value);
        }

        public void SetValueWithoutNotify(float value)
        {
            SetValue(value, false);
        }
    }
}
