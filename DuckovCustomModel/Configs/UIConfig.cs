using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public enum AnchorPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public class UIConfig : ConfigBase
    {
        public KeyCode ToggleKey { get; set; } = KeyCode.Backslash;
        public KeyCode AnimatorParamsToggleKey { get; set; } = KeyCode.None;
        public KeyCode EmotionModifierKey1 { get; set; } = KeyCode.LeftShift;
        public KeyCode EmotionModifierKey2 { get; set; } = KeyCode.RightShift;
        public bool ShowDCMButton { get; set; } = true;
        public AnchorPosition DCMButtonAnchor { get; set; } = AnchorPosition.TopLeft;
        public float DCMButtonOffsetX { get; set; } = 10f;
        public float DCMButtonOffsetY { get; set; } = -10f;

        public override void LoadDefault()
        {
            ToggleKey = KeyCode.Backslash;
            AnimatorParamsToggleKey = KeyCode.None;
            EmotionModifierKey1 = KeyCode.LeftShift;
            EmotionModifierKey2 = KeyCode.RightShift;
            ShowDCMButton = true;
            DCMButtonAnchor = AnchorPosition.TopLeft;
            DCMButtonOffsetX = 10f;
            DCMButtonOffsetY = -10f;
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UIConfig otherSetting) return;
            ToggleKey = otherSetting.ToggleKey;
            AnimatorParamsToggleKey = otherSetting.AnimatorParamsToggleKey;
            EmotionModifierKey1 = otherSetting.EmotionModifierKey1;
            EmotionModifierKey2 = otherSetting.EmotionModifierKey2;
            ShowDCMButton = otherSetting.ShowDCMButton;
            DCMButtonAnchor = otherSetting.DCMButtonAnchor;
            DCMButtonOffsetX = otherSetting.DCMButtonOffsetX;
            DCMButtonOffsetY = otherSetting.DCMButtonOffsetY;
        }
    }
}
