using UnityEngine;
using UnityEngine.Serialization;

namespace MagicGrass
{
    public class InteractObjectManager : MonoBehaviour
    {
        private struct InteractObjectData
        {
            public Vector3 Pos;
            public float Type;
            public float Radius;
            public float TrailRecoverSpeed;
        }

        [FormerlySerializedAs("m_objects")] [SerializeField] private InteractableObject[] _objects;
        private InteractObjectData[] _objectDatas;
        private ComputeBuffer _objectPositionsBuffer;
        private MagicGrassSettingAsset _magicGrassSetting;

        public void OnEnable()
        {
            if (_magicGrassSetting == null)
                _magicGrassSetting = Resources.Load<MagicGrassSettingAsset>("MagicGrassSettingAsset");
            _objectPositionsBuffer = new ComputeBuffer(Mathf.Max(_objects.Length, 1), sizeof(float) * 3 * 2);
            _objectDatas = new InteractObjectData[_objects.Length];
        }

        public void OnDisable()
        {
            _objectPositionsBuffer.Release();
        }

        public void Update()
        {
            for (int i = 0; i < _objects.Length; i++)
            {
                _objectDatas[i].Pos = _objects[i].transform.position;
                _objectDatas[i].TrailRecoverSpeed =
                    _magicGrassSetting.GetRecoverSpeedByInteractType(_objects[i].Type);
                _objectDatas[i].Type = Mathf.Floor((float)((int)_objects[i].Type));
                _objectDatas[i].Radius = 0.5f * (int)_objects[i].transform.localScale.x;
            }

            _objectPositionsBuffer.SetData(_objectDatas);
            Shader.SetGlobalBuffer(InteractMotionBufferController.ImagePaintProperty.s_objectPositionWSBuffer,
                _objectPositionsBuffer);
            Shader.SetGlobalInt(InteractMotionBufferController.ImagePaintProperty.s_numberOfObjects, _objects.Length);
        }
    }
}