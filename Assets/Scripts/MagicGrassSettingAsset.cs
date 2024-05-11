using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MagicGrass
{
    [CreateAssetMenu(menuName = "MagicGrass/MagicGrassSettingAsset")]
    public class MagicGrassSettingAsset : ScriptableObject
    {
        [SerializeField] [Range(1f, 200f)] public float CloseDistance = 50;
        [SerializeField] [Range(2f, 200f)] public float MidDistance = 90;
        [SerializeField] [Range(3f, 200f)] public float FarDistance = 150;

        [Header("Interact Behavior")] [Range(0, 2f)]
        public float PushBehaviorRecoverSpeed = 0.3f;

        [Range(0, 2f)] public float AbsorbBehaviorRecoverSpeed = 0.3f;
        [Range(0, 2f)] public float BurnningBehaviorRecoverSpeed = 0.3f;
        [Range(0, 2f)] public float FreezeBehaviorRecoverSpeed = 0.3f;

        public float GetRecoverSpeedByInteractType(InteractMotionBufferController.GrassInteractorType type)
        {
            if (type == InteractMotionBufferController.GrassInteractorType.WalkPusher)
            {
                return PushBehaviorRecoverSpeed;
            }
            else if (type == InteractMotionBufferController.GrassInteractorType.Absorb)
            {
                return AbsorbBehaviorRecoverSpeed;
            }
            else if (type == InteractMotionBufferController.GrassInteractorType.Burnning)
            {
                return BurnningBehaviorRecoverSpeed;
            }
            else if (type == InteractMotionBufferController.GrassInteractorType.Freeze)
            {
                return FreezeBehaviorRecoverSpeed;
            }

            return 0f;
        }


        public void OnValidate()
        {
            CloseDistance = Mathf.Clamp(CloseDistance, 1f, Mathf.Min(MidDistance, FarDistance) - 1f);
            MidDistance = Mathf.Clamp(MidDistance, CloseDistance + 1, FarDistance - 1);
            FarDistance = Mathf.Clamp(FarDistance, MidDistance + 1, 200f);
        }
    }
}