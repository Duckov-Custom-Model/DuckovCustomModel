using System.Collections.Generic;

namespace DuckovCustomModel.Data
{
    public class SoundInfo
    {
        public string Path { get; set; } = string.Empty;
        public string[] Tags { get; set; } = [];

        public void Initialize()
        {
            if (Tags is not { Length: > 0 })
            {
                Tags = [Constant.SoundTagNormal];
                return;
            }

            var tagSet = new List<string>();
            foreach (var tag in Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var normalizedTag = tag.ToLowerInvariant().Trim();
                if (normalizedTag is Constant.SoundTagNormal or Constant.SoundTagSurprise or Constant.SoundTagDeath)
                    tagSet.Add(normalizedTag);
            }

            if (tagSet.Count == 0) tagSet.Add(Constant.SoundTagNormal);
            Tags = tagSet.ToArray();
        }
    }
}