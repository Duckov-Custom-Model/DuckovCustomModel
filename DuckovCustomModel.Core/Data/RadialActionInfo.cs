using System;

namespace DuckovCustomModel.Core.Data
{
    public sealed class RadialActionInfo
    {
        public const string TriggerMode = "Trigger";
        public const string BoolMode = "Bool";
        public const string IntMode = "Int";
        public const string FloatMode = "Float";

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Parameter { get; set; } = string.Empty;
        public string Mode { get; set; } = TriggerMode;
        public float Value { get; set; }
        public RadialFormInfo[] Forms { get; set; } = [];

        public bool Validate()
        {
            Id = Id?.Trim() ?? string.Empty;
            Name = Name?.Trim() ?? string.Empty;
            Parameter = Parameter?.Trim() ?? string.Empty;
            if (Id.Length == 0 || Parameter.Length == 0) return false;
            if (Name.Length == 0) Name = Id;
            if (string.Equals(Mode, TriggerMode, StringComparison.OrdinalIgnoreCase))
                Mode = TriggerMode;
            else if (string.Equals(Mode, BoolMode, StringComparison.OrdinalIgnoreCase))
                Mode = BoolMode;
            else if (string.Equals(Mode, IntMode, StringComparison.OrdinalIgnoreCase))
                Mode = IntMode;
            else if (string.Equals(Mode, FloatMode, StringComparison.OrdinalIgnoreCase))
                Mode = FloatMode;
            else
                return false;
            if (float.IsNaN(Value) || float.IsInfinity(Value) ||
                Mode == IntMode && (Value < int.MinValue || Value > int.MaxValue || Value != (int)Value))
                return false;
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var validForms = new System.Collections.Generic.List<RadialFormInfo>();
            foreach (var form in Forms ?? [])
                if (form != null && form.Validate() && ids.Add(form.Id)) validForms.Add(form);
            Forms = validForms.ToArray();
            return true;
        }
    }
}
