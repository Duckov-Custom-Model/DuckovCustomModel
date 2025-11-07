namespace DuckovCustomModel.Configs
{
    public class UsingModel : ConfigBase
    {
        public string ModelID { get; set; } = string.Empty;

        public string PetModelID { get; set; } = string.Empty;

        public override void LoadDefault()
        {
            ModelID = string.Empty;
            PetModelID = string.Empty;
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UsingModel otherSetting) return;
            ModelID = otherSetting.ModelID;
            PetModelID = otherSetting.PetModelID;
        }
    }
}