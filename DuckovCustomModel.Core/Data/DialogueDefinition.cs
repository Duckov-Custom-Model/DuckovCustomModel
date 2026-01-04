using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DuckovCustomModel.Core.Data
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DialoguePlayMode
    {
        Sequential,
        Random,
        RandomNoRepeat,
        Continuous,
    }

    public class DialogueDefinition
    {
        private readonly object _lock = new();
        public string Id { get; set; } = string.Empty;
        public string[] Texts { get; set; } = [];
        public DialoguePlayMode Mode { get; set; } = DialoguePlayMode.Sequential;
        public float Duration { get; set; } = 2f;

        [JsonIgnore]
        [field: JsonIgnore]
        public int CurrentIndex
        {
            get
            {
                lock (_lock)
                {
                    return field;
                }
            }
            set
            {
                lock (_lock)
                {
                    field = value;
                }
            }
        }

        [JsonIgnore]
        [field: JsonIgnore]
        public List<int> RemainingIndices
        {
            get
            {
                lock (_lock)
                {
                    field ??= [];
                    return field;
                }
            }
            set
            {
                lock (_lock)
                {
                    field = value;
                }
            }
        }
    }
}
