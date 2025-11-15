using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.Data.CustomAnimation
{
    public class CustomAnimationParameters
    {
        public List<ParameterData> Parameters { get; set; } = [];

        public class ParameterData
        {
            public string Name { get; set; } = string.Empty;
            public float DefaultValue { get; set; } = 0f;
            public AnimatorControllerParameterType Type { get; set; } = AnimatorControllerParameterType.Float;
        }
    }
}
