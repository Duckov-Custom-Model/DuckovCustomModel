using System;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Configs
{
    public interface IConfigBase : IValidatable, ICloneable
    {
        void LoadDefault();
        void LoadFromFile(string filePath, bool autoSaveOnLoad = true);
        void SaveToFile(string filePath, bool withBackup = false);
        void CopyFrom(IConfigBase other);
    }
}
