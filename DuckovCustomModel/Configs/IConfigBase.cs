namespace DuckovCustomModel.Configs
{
    public interface IConfigBase
    {
        void LoadDefault();
        void LoadFromFile(string filePath, bool autoSaveOnLoad = true);
        void SaveToFile(string filePath, bool withBackup = true);
        bool Validate();
        IConfigBase Clone();
        void CopyFrom(IConfigBase other);
    }
}