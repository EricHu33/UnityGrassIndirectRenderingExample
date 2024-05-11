using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MagicGrass
{
    public class InteractMotionBufferController : MonoBehaviour
    {
        public enum GrassInteractorType
        {
            WalkPusher = 1,
            Explosion = 2,
            Absorb = 3,
            Burnning = 4,
            Freeze = 5,
        }

        public class ImagePaintProperty
        {
            public static int s_deltaTime = Shader.PropertyToID("_DeltaTime");
            public static int s_objectPositionWSBuffer = Shader.PropertyToID("_ObjectPositionWSBuffer");
            public static int s_numberOfObjects = Shader.PropertyToID("_NumberOfObjects");
            public static int s_terrainSize = Shader.PropertyToID("_TerrainSize");
            public static int s_interactMotionCenterWS = Shader.PropertyToID("_InteractMotionCenterWS");
            public static int s_interactPrograssMap = Shader.PropertyToID("_InteractProgressMap");
            public static int s_interactMap = Shader.PropertyToID("_InteractMap");
            public static int s_heightMap = Shader.PropertyToID("_HeightMap");
            public static int s_twiceTerrainHeight = Shader.PropertyToID("_TwiceTerrainHeight");
            public static int s_interactMapSize = Shader.PropertyToID("_InteractMapSize");
            public static int s_cellSize = Shader.PropertyToID("_CellSize");
            public static int s_cellId = Shader.PropertyToID("_CellId");
        }

        private ComputeShader _imagePaintCS;
        [FormerlySerializedAs("m_interactMap")] [ReadOnly, SerializeField] private RenderTexture _interactMap;
        public RenderTexture InteractBuffer => _interactMap;
        
        private float _terrainSize;
        private float _terrainHeight;
        private RenderTexture _terrainHeightMap;
        private const int RENDER_TEXTURE_SIZE = 512;
        public float MapSize => (float)RENDER_TEXTURE_SIZE;
        private int _cellSize;
        private const int CELL_COUNT_PER_EDGE = 4;
        private Bounds[] _cellBounds;
        private BoundingSphere[] _cellBoundingSpheres;
        private bool[] _cellStatuses;
        private float[] _distanceBands = new float[] { 10, 50, 150 };
        private CullingGroup _cullingGroup;

        [ReadOnly, SerializeField] private MagicGrassSettingAsset m_magicGrassSetting;
        private bool _isReady = false;

        public void Init(MagicGrassSettingAsset magicGrassSetting, float terrainSize, float terrainHeight,
            RenderTexture heightMap)
        {
            m_magicGrassSetting = magicGrassSetting;
            _terrainSize = terrainSize;
            _terrainHeight = terrainHeight;
            _terrainHeightMap = heightMap;
            _interactMap =
                new RenderTexture(RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 0, RenderTextureFormat.ARGBHalf);
            _interactMap.enableRandomWrite = true;

            _imagePaintCS = Instantiate((Resources.Load<ComputeShader>("ImagePaint")));

            _cellSize = RENDER_TEXTURE_SIZE / CELL_COUNT_PER_EDGE;
            _cellBounds = new Bounds[CELL_COUNT_PER_EDGE * CELL_COUNT_PER_EDGE];
            _cellBoundingSpheres = new BoundingSphere[CELL_COUNT_PER_EDGE * CELL_COUNT_PER_EDGE];
            _cellStatuses = new bool[CELL_COUNT_PER_EDGE * CELL_COUNT_PER_EDGE];
            for (int x = 0; x < CELL_COUNT_PER_EDGE; x++)
            {
                for (int y = 0; y < CELL_COUNT_PER_EDGE; y++)
                {
                    var sampleUV = new Vector2((x * _cellSize + _cellSize * 0.5f) / (float)RENDER_TEXTURE_SIZE,
                        (y * _cellSize + _cellSize * 0.5f) / (float)RENDER_TEXTURE_SIZE);
                    var centerWS = GetComponent<Terrain>().transform.position +
                                   new Vector3(sampleUV.x, 0, sampleUV.y) * terrainSize;
                    var index = x * CELL_COUNT_PER_EDGE + y;
                    _cellBounds[index] = new Bounds();
                    _cellBounds[index].center = centerWS;
                    var cellToMapScale = (float)_cellSize / (float)RENDER_TEXTURE_SIZE;
                    _cellBounds[index].extents = new Vector3(cellToMapScale, 0, cellToMapScale) * terrainSize * 0.5f;
                }
            }

            for (int i = 0; i < _cellBounds.Length; i++)
            {
                _cellBoundingSpheres[i] =
                    new BoundingSphere(_cellBounds[i].center, _cellBounds[i].extents.x * 1.41f);
                _cellStatuses[i] = false;
            }

            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = Camera.main;
            _cullingGroup.SetBoundingSpheres(_cellBoundingSpheres);
            _cullingGroup.SetDistanceReferencePoint(Camera.main.transform);
            _cullingGroup.SetBoundingDistances(_distanceBands);
            _cullingGroup.onStateChanged = OnCullingStateChange;
            _isReady = true;
        }

        private void OnCullingStateChange(CullingGroupEvent cullingGroupEvent)
        {
            _cellStatuses[cullingGroupEvent.index] =
                cullingGroupEvent.isVisible || cullingGroupEvent.currentDistance <= 2;
        }

        public void Clear()
        {
            _isReady = false;
            _interactMap.Release();
            if (_cullingGroup != null)
                _cullingGroup.Dispose();
        }

        private void Update()
        {
            if (!_isReady)
                return;

            for (int x = 0; x < CELL_COUNT_PER_EDGE; x++)
            {
                for (int y = 0; y < CELL_COUNT_PER_EDGE; y++)
                {
                    var index = x * CELL_COUNT_PER_EDGE + y;
                    if (_cellStatuses[index])
                    {
                        UpdateCell(x, y);
                    }
                }
            }
        }

        // Update is called once per frame
        void UpdateCell(int cellX, int cellY)
        {
            if (_interactMap == null)
            {
                Debug.LogWarning("interact buffer not initialize yet");
                return;
            }

            _imagePaintCS.SetFloat(ImagePaintProperty.s_interactMapSize, RENDER_TEXTURE_SIZE);
            _imagePaintCS.SetFloat(ImagePaintProperty.s_cellSize, _cellSize);
            _imagePaintCS.SetVector(ImagePaintProperty.s_cellId, new Vector2(cellX, cellY));
            _imagePaintCS.SetFloat(ImagePaintProperty.s_deltaTime, Time.deltaTime);
            _imagePaintCS.SetVector(ImagePaintProperty.s_interactMotionCenterWS, transform.position);
            _imagePaintCS.SetTexture(0, ImagePaintProperty.s_interactMap, _interactMap);
            _imagePaintCS.SetFloat(ImagePaintProperty.s_terrainSize, _terrainSize);
            _imagePaintCS.SetFloat(ImagePaintProperty.s_twiceTerrainHeight, _terrainHeight * 2f);
            _imagePaintCS.SetTexture(0, ImagePaintProperty.s_heightMap, _terrainHeightMap);
            _imagePaintCS.Dispatch(0, Mathf.CeilToInt((float)_cellSize / 32f), Mathf.CeilToInt((float)_cellSize / 32f),
                1);
        }
    }
}