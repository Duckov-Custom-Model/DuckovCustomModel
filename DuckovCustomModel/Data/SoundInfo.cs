using System.Collections.Generic;
using Newtonsoft.Json;

namespace DuckovCustomModel.Data
{
    public class SoundInfo
    {
        public string Path { get; set; } = string.Empty;
        public string[] Tags { get; set; } = [];

        [JsonIgnore] public HashSet<string> TagSet { get; private set; } = [];

        public void Initialize()
        {
            TagSet.Clear();
            if (Tags is not { Length: > 0 })
            {
                TagSet = ["normal"];
                return;
            }

            foreach (var tag in Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var normalizedTag = tag.ToLowerInvariant().Trim();
                if (normalizedTag is "normal" or "surprise" or "death")
                    TagSet.Add(normalizedTag);
            }

            if (TagSet.Count == 0) TagSet.Add("normal");
        }

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && TagSet.Contains(tag.ToLowerInvariant().Trim());
        }
    }
}