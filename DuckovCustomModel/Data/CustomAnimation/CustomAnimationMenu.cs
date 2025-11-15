namespace DuckovCustomModel.Data.CustomAnimation
{
    public class CustomAnimationMenu
    {
        public const int MaxControls = 8;
        public Control[] Controls { get; set; } = [];

        public class Control
        {
            public enum ControlType
            {
                Button,
                Toggle,
                SubMenu,
                TwoAxisPuppet,
                FourAxisPuppet,
                RadialPuppet,
            }

            public string Name { get; set; } = string.Empty;
            public string IconPath { get; set; } = string.Empty;
            public ControlType Type { get; set; } = ControlType.Button;
            public CustomAnimationMenu? SubMenu { get; set; } = null;
            public string[]? SubParameterNames { get; set; } = null;
        }
    }
}
