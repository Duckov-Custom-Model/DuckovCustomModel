using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DuckovCustomModel.Core
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerSettings Default = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = [new StringEnumConverter()],
        };
    }
}
