using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public class UIConfig : ConfigBase
    {
        public KeyCode ToggleKey { get; set; } = KeyCode.Backslash;
        public bool HideCharacterEquipment { get; set; }
        public bool HidePetEquipment { get; set; }

        public override void LoadDefault()
        {
            ToggleKey = KeyCode.Backslash;
            HideCharacterEquipment = false;
            HidePetEquipment = false;
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UIConfig otherSetting) return;
            ToggleKey = otherSetting.ToggleKey;
            HideCharacterEquipment = otherSetting.HideCharacterEquipment;
            HidePetEquipment = otherSetting.HidePetEquipment;
        }
    }
}