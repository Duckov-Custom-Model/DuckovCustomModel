using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public class UIConfig : ConfigBase
    {
        public KeyCode ToggleKey { get; set; } = KeyCode.Equals;

        public override void LoadDefault()
        {
            ToggleKey = KeyCode.Equals;
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UIConfig otherSetting) return;
            ToggleKey = otherSetting.ToggleKey;
        }
    }
}