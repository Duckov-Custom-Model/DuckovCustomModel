using System;
using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public sealed class RadialFormInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Parameter { get; set; } = string.Empty;
        public string Mode { get; set; } = RadialActionInfo.BoolMode;
        public float Min { get; set; }
        public float Max { get; set; } = 1;
        public float Step { get; set; }
        public RadialFormOptionInfo[] Options { get; set; } = [];

        public bool Validate()
        {
            Id = Id?.Trim() ?? string.Empty;
            Name = Name?.Trim() ?? string.Empty;
            Parameter = Parameter?.Trim() ?? string.Empty;
            if (Id.Length == 0 || Parameter.Length == 0) return false;
            if (Name.Length == 0) Name = Id;
            if (string.Equals(Mode, RadialActionInfo.BoolMode, StringComparison.OrdinalIgnoreCase))
                Mode = RadialActionInfo.BoolMode;
            else if (string.Equals(Mode, RadialActionInfo.IntMode, StringComparison.OrdinalIgnoreCase))
                Mode = RadialActionInfo.IntMode;
            else if (string.Equals(Mode, RadialActionInfo.FloatMode, StringComparison.OrdinalIgnoreCase))
                Mode = RadialActionInfo.FloatMode;
            else return false;
            if (float.IsNaN(Min) || float.IsInfinity(Min) || float.IsNaN(Max) || float.IsInfinity(Max) ||
                float.IsNaN(Step) || float.IsInfinity(Step) || Step < 0 || Max < Min)
                return false;
            var options = new List<RadialFormOptionInfo>();
            var values = new HashSet<int>();
            foreach (var option in Options ?? [])
                if (option != null && option.Validate() && values.Add(option.Value)) options.Add(option);
            Options = options.ToArray();
            return Mode != RadialActionInfo.IntMode || Options.Length > 0 ||
                   Min >= int.MinValue && Max <= int.MaxValue;
        }
    }

    public sealed class RadialFormOptionInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        public bool Validate()
        {
            Name = Name?.Trim() ?? string.Empty;
            return Name.Length > 0;
        }
    }
}
