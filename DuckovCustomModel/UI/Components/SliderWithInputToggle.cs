using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class SliderWithInputToggle : MonoBehaviour
    {
        private TMP_InputField? _inputField;
        private float _maxValue;
        private float _minValue;
        private Slider? _slider;
        private string _valueFormat = "F3";

        public float Value
        {
            get => _slider?.value ?? _minValue;
            set => SetValue(value, true);
        }

        public event UnityAction<float>? OnValueChanged;

        public void Initialize(Slider slider, TMP_InputField inputField, float minValue, float maxValue,
            float initialValue, string valueFormat, UnityAction<float>? onValueChanged)
        {
            _slider = slider;
            _inputField = inputField;
            _minValue = minValue;
            _maxValue = maxValue;
            _valueFormat = valueFormat;
            if (onValueChanged != null) OnValueChanged += onValueChanged;
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
            _inputField.onEndEdit.AddListener(OnInputEndEdit);
            _inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            SetValue(initialValue, false);
        }

        private void OnSliderValueChanged(float value)
        {
            _inputField?.SetTextWithoutNotify(value.ToString(_valueFormat, CultureInfo.InvariantCulture));
            OnValueChanged?.Invoke(value);
        }

        private void OnInputEndEdit(string text)
        {
            if (_inputField != null && !_inputField.wasCanceled &&
                (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                 float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) &&
                !float.IsNaN(value) && !float.IsInfinity(value))
                SetValue(value, true);
            else
                SetValue(Value, false);
        }

        private void SetValue(float value, bool notify)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            value = Mathf.Clamp(value, _minValue, _maxValue);
            var changed = !Mathf.Approximately(Value, value);
            _slider?.SetValueWithoutNotify(value);
            _inputField?.SetTextWithoutNotify(value.ToString(_valueFormat, CultureInfo.InvariantCulture));
            if (notify && changed) OnValueChanged?.Invoke(value);
        }

        public void SetValueWithoutNotify(float value)
        {
            SetValue(value, false);
        }
    }
}
