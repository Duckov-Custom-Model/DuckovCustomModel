using UnityEngine;

namespace DuckovCustomModel.Data
{
    public static class CustomAnimatorHash
    {
        public static readonly int CurrentCharacterType = Animator.StringToHash("CurrentCharacterType");

        public static readonly int Grounded = Animator.StringToHash("Grounded");
        public static readonly int Die = Animator.StringToHash("Die");
        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        public static readonly int MoveDirX = Animator.StringToHash("MoveDirX");
        public static readonly int MoveDirY = Animator.StringToHash("MoveDirY");
        public static readonly int IsMoving = Animator.StringToHash("Moving");
        public static readonly int IsRunning = Animator.StringToHash("Running");
        public static readonly int Dashing = Animator.StringToHash("Dashing");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int HandState = Animator.StringToHash("HandState");
        public static readonly int GunReady = Animator.StringToHash("GunReady");
        public static readonly int Reloading = Animator.StringToHash("Reloading");
        public static readonly int RightHandOut = Animator.StringToHash("RightHandOut");

        public static readonly int HealthRate = Animator.StringToHash("HealthRate");
        public static readonly int WaterRate = Animator.StringToHash("WaterRate");
        public static readonly int WeightState = Animator.StringToHash("WeightState");
        public static readonly int WeightRate = Animator.StringToHash("WeightRate");

        public static readonly int HideOriginalEquipment = Animator.StringToHash("HideOriginalEquipment");
        public static readonly int LeftHandEquip = Animator.StringToHash("LeftHandEquip");
        public static readonly int RightHandEquip = Animator.StringToHash("RightHandEquip");
        public static readonly int ArmorEquip = Animator.StringToHash("ArmorEquip");
        public static readonly int HelmetEquip = Animator.StringToHash("HelmetEquip");
        public static readonly int HeadsetEquip = Animator.StringToHash("HeadsetEquip");
        public static readonly int FaceEquip = Animator.StringToHash("FaceEquip");
        public static readonly int BackpackEquip = Animator.StringToHash("BackpackEquip");
        public static readonly int MeleeWeaponEquip = Animator.StringToHash("MeleeWeaponEquip");
        public static readonly int HavePopText = Animator.StringToHash("HavePopText");

        public static readonly int LeftHandTypeID = Animator.StringToHash("LeftHandTypeID");
        public static readonly int RightHandTypeID = Animator.StringToHash("RightHandTypeID");
        public static readonly int ArmorTypeID = Animator.StringToHash("ArmorTypeID");
        public static readonly int HelmetTypeID = Animator.StringToHash("HelmetTypeID");
        public static readonly int HeadsetTypeID = Animator.StringToHash("HeadsetTypeID");
        public static readonly int FaceTypeID = Animator.StringToHash("FaceTypeID");
        public static readonly int BackpackTypeID = Animator.StringToHash("BackpackTypeID");
        public static readonly int MeleeWeaponTypeID = Animator.StringToHash("MeleeWeaponTypeID");
    }
}