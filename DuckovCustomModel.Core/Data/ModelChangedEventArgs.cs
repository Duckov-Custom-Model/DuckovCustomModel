namespace DuckovCustomModel.Core.Data
{
    public class ModelChangedEventArgs
    {
        public ModelTarget Target { get; set; }
        public string? AICharacterNameKey { get; set; }
        public string? ModelID { get; set; }
        public string? ModelName { get; set; }
        public bool IsRestored { get; set; }
        public bool Success { get; set; }
        public int HandlerCount { get; set; }
    }
}
