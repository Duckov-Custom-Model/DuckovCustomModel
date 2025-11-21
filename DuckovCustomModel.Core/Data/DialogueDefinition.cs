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
        [JsonIgnore] private int _currentIndex;
        [JsonIgnore] private List<int>? _remainingIndices;
        public string Id { get; set; } = string.Empty;
        public string[] Texts { get; set; } = [];
        public DialoguePlayMode Mode { get; set; } = DialoguePlayMode.Sequential;
        public float Duration { get; set; } = 2f;

        [JsonIgnore]
        public int CurrentIndex
        {
            get
            {
                lock (_lock)
                {
                    return _currentIndex;
                }
            }
            set
            {
                lock (_lock)
                {
                    _currentIndex = value;
                }
            }
        }

        [JsonIgnore]
        public List<int> RemainingIndices
        {
            get
            {
                lock (_lock)
                {
                    _remainingIndices ??= [];
                    return _remainingIndices;
                }
            }
            set
            {
                lock (_lock)
                {
                    _remainingIndices = value;
                }
            }
        }
    }
}
