using System;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Core.Data
{
    public class ModelChangedEventArgs
    {
        public Object? Handler { get; set; }
        public string TargetTypeId { get; set; } = string.Empty;
        public string? ModelID { get; set; }
        public string? ModelName { get; set; }
        public bool IsRestored { get; set; }

        [Obsolete("Use TargetTypeId instead. This property is kept for backward compatibility.")]
        public ModelTarget Target { get; set; }

        [Obsolete("Use TargetTypeId instead. This property is kept for backward compatibility.")]
        public string? AICharacterNameKey { get; set; }

        [Obsolete("Success is no longer used. This property is kept for backward compatibility.")]
        public bool Success { get; set; }

        [Obsolete(
            "HandlerCount is no longer accurate in event-driven model application. This property is kept for backward compatibility.")]
        public int HandlerCount { get; set; }
    }
}
