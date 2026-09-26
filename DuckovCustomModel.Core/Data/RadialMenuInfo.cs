using System;

namespace DuckovCustomModel.Core.Data
{
    public sealed class RadialMenuInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public RadialActionInfo[] Actions { get; set; } = [];
        public RadialMenuInfo[] Menus { get; set; } = [];

        public bool Validate()
        {
            Id = Id?.Trim() ?? string.Empty;
            Name = Name?.Trim() ?? string.Empty;
            if (Id.Length == 0) return false;
            if (Name.Length == 0) Name = Id;
            return true;
        }
    }
}
