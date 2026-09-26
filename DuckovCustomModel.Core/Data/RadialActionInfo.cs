using System;

namespace DuckovCustomModel.Core.Data
{
    public sealed class RadialActionInfo
    {
        public const string TriggerMode = "Trigger";
        public const string BoolMode = "Bool";

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Parameter { get; set; } = string.Empty;
        public string Mode { get; set; } = TriggerMode;

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
            else
                return false;
            return true;
        }
    }
}
