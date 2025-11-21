using System.Linq;

namespace DuckovCustomModel.Core.Data
{
    public class SoundInfo
    {
        public string Path { get; set; } = string.Empty;
        public string[] Tags { get; set; } = [];

        public void Initialize()
        {
            if (Tags is not { Length: > 0 })
            {
                Tags = [SoundTags.Normal];
                return;
            }

            var tagSet = (from tag in Tags
                where !string.IsNullOrWhiteSpace(tag)
                select tag.ToLowerInvariant().Trim()).ToList();

            if (tagSet.Count == 0) tagSet.Add(SoundTags.Normal);
            Tags = tagSet.ToArray();
        }
    }
}
