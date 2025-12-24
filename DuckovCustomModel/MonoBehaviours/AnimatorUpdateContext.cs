using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class AnimatorUpdateContext
    {
        public bool Initialized { get; set; }
        public CharacterMainControl? CharacterMainControl { get; set; }
        public CharacterModel? CharacterModel { get; set; }
        public ModelHandler? ModelHandler { get; set; }
        public DuckovItemAgent? HoldAgent { get; set; }
        public ItemAgent_Gun? GunAgent { get; set; }
        public bool Attacking { get; set; }
        public float AttackTimer { get; set; }
        public float AttackWeight { get; set; }
        public bool HasAnimationIfDashCanControl { get; set; }
        public AnimationCurve? AttackLayerWeightCurve { get; set; }
        public float AttackTime { get; set; }
    }
}
