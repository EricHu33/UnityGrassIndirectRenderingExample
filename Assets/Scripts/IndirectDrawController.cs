using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System;
using System.Linq;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEngine.TestTools;
using Unity.Rendering.Universal;
using UnityEditor;
#endif

namespace MagicGrass
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(InteractMotionBufferController))]
    [RequireComponent(typeof(RuntimeMapsController))]
    [RequireComponent(typeof(PaintTextureController))]
    [RequireComponent(typeof(Terrain))]
    public class IndirectDrawController : MonoBehaviour
    {
        static readonly ProfilerMarker s_markGrassSyncLod = new ProfilerMarker("SyncLOD");
        static readonly ProfilerMarker s_markGrassRendererRender1 = new ProfilerMarker("MagicGrass.Render.Renderer.1");
        static readonly ProfilerMarker s_markGrassRendererRender2 = new ProfilerMarker("MagicGrass.Render.Renderer.2");
        static readonly ProfilerMarker s_markGrassRendererRender3 = new ProfilerMarker("MagicGrass.Render.Renderer.3");
        static readonly ProfilerMarker s_markGrassUpdate = new ProfilerMarker("MagicGrass.CPU.Update");

        [System.Serializable]
        public class MassGrassRenderer
        {
            private MaterialProperty[] _cachedProperties;
            private Material[] _sourceMaterials;
            private Material _sourceMaterial;
            private Material _runtimeMaterial;
            private ComputeShader _frustumCullingCS;
            private ComputeShader _drawCS;
            private ComputeBuffer _drawedGrassBuferr;
            private ComputeBuffer _cullingCounterBuffer;
            private ComputeBuffer _argsBuffer;
            private ComputeBuffer _culledResultBuffer;
            private uint[] _totalGrassCounter = new uint[1] { 0 };
            private ComputeBuffer _cullInputCounterBuffer;
            private int _grassDensity;
            private uint[] _cullingCounterArgs = new uint[5] { 0, 0, 0, 0, 0 };
            private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
            private Mesh _mesh;
            private MagicGrassGrassMapSize _mapSize;
            private int _cellSize;
            private int _cellX;
            private int _cellY;
            private Vector3 _terrainPivotWS;
            private float _terrainSizeWS;
            private Bounds _cellBounds;
            private RenderTexture _hizBuffer;
            private RenderTexture _groundTintMap;
            public Bounds CellBounds => _cellBounds;
            public int ComputeBufferSize => _grassDensity * _cellSize * _cellSize;
            public bool HasCached => _drawedGrassBuferr != null && _drawedGrassBuferr.IsValid();

            public Bounds GetCellCenter()
            {
                return GetCellCenter((int)_mapSize, _cellSize, _cellX, _cellY, _terrainSizeWS);
            }

            public Bounds GetCellCenter(int mapSize, int cellSize, int cellIdX, int cellIdY, float terrainSizeWS)
            {
                var sampleUV = new Vector2((cellSize * cellIdX + cellSize * 0.5f) / (float)mapSize,
                    ((cellSize * cellIdY + cellSize * 0.5f) / (float)mapSize));
                var center = _terrainPivotWS + new Vector3(sampleUV.x, 0, sampleUV.y) * terrainSizeWS;
                var bounds = new Bounds();
                var cellToMapScale = (cellSize / (float)mapSize);
                bounds.center = center;
                bounds.extents = new Vector3(cellToMapScale, 0, cellToMapScale) * terrainSizeWS * 0.5f;
                return bounds;
            }

            public void SetCellInfo(int cellSize, int cellX, int cellY, MagicGrassGrassMapSize mapSize,
                Vector3 terrainCenter, float terrainSize)
            {
                _mapSize = mapSize;
                _cellSize = cellSize;
                _cellX = cellX;
                _cellY = cellY;
                _terrainPivotWS = terrainCenter;
                _terrainSizeWS = terrainSize;
            }

            public void Init(int cellSize, int cellX, int cellY, MagicGrassGrassMapSize mapSize, int randomSeed,
                Terrain terrain,
                Texture2D[] detailMaps, int detailMapIndex, Vector4 detailSplatChannels, RenderTexture heightMap,
                RenderTexture normalMap, RenderTexture surfaceColorMap, Texture2DArray terrainAlphaMaps,
                HashSet<int> terrainExcludeLayerIndexes, HashSet<int> terrainIncludeLayerIndexes, int grassDensity,
                Mesh lod0Mesh, Material material, ComputeShader drawCS, ComputeShader frustumCullCS)
            {
                _sourceMaterials = new Material[1];
                _sourceMaterials[0] = material;
#if UNITY_EDITOR
                _cachedProperties = MaterialEditor.GetMaterialProperties(_sourceMaterials);
#endif
                _groundTintMap = surfaceColorMap;
                SetCellInfo(cellSize, cellX, cellY, mapSize, terrain.transform.position, terrain.terrainData.size.x);
                var sizeOfThreadsXY = (int)cellSize * (int)cellSize;
                _drawCS = Instantiate(drawCS);
                _drawCS.SetInt("_RandomSeed", randomSeed);

                var excludeControls0 = Vector4.zero;
                var excludeControls1 = Vector4.zero;
                foreach (var layerIndex in terrainExcludeLayerIndexes)
                {
                    var channel = layerIndex % 4;
                    if (layerIndex < 4)
                    {
                        excludeControls0[channel] = excludeControls0[channel] == 0 ? 1f : excludeControls0[channel];
                    }
                    else if (layerIndex >= 4 && layerIndex < 8)
                    {
                        excludeControls1[channel] = excludeControls1[channel] == 0 ? 1f : excludeControls1[channel];
                    }
                }

                var includeControls0 = Vector4.zero;
                var includeControls1 = Vector4.zero;
                foreach (var layerIndex in terrainIncludeLayerIndexes)
                {
                    var channel = layerIndex % 4;
                    if (layerIndex < 4)
                    {
                        includeControls0[channel] = includeControls0[channel] == 0 ? 1f : includeControls0[channel];
                    }
                    else if (layerIndex >= 4 && layerIndex < 8)
                    {
                        includeControls1[channel] = includeControls1[channel] == 0 ? 1f : includeControls1[channel];
                    }
                }

                _drawCS.SetFloat("_CellSize", (float)cellSize);
                _drawCS.SetVector("_ExcludeControls0", excludeControls0);
                _drawCS.SetVector("_ExcludeControls1", excludeControls1);

                _drawCS.SetVector("_IncludeControls0", includeControls0);
                _drawCS.SetVector("_IncludeControls1", includeControls1);
                _drawCS.SetVector(MagicGrassProperty._SplatChannels, detailSplatChannels);

                _drawCS.SetVector("_CellId", new Vector2(cellX, cellY));
                _drawCS.SetTexture(0, MagicGrassProperty._HeightMap, heightMap);
                _drawCS.SetTexture(0, "_DetailMap", detailMaps[detailMapIndex]);
                _runtimeMaterial = Instantiate(material);
                _frustumCullingCS = Instantiate(frustumCullCS);
                //TODO use tex2dArray alphamaps
                _drawCS.SetTexture(0, "_TerrainAlphaMaps", terrainAlphaMaps);
                _drawCS.SetTexture(0, "_NormalMap", normalMap);
                _drawCS.SetFloat(MagicGrassProperty._TwiceTerrainHeight, terrain.terrainData.size.y * 2f);
                _mesh = lod0Mesh;
                _grassDensity = grassDensity;
                _sourceMaterial = material;
                _frustumCullingCS.SetInt("_GrassXYThreadsSize", (int)cellSize);
                _cullInputCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
                _cullingCounterBuffer =
                    new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

                _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                if (lod0Mesh != null)
                {
                    _args[0] = (uint)lod0Mesh.GetIndexCount(0);
                    _args[1] = (uint)(sizeOfThreadsXY * grassDensity);
                    _args[2] = (uint)lod0Mesh.GetIndexStart(0);
                    _args[3] = (uint)lod0Mesh.GetBaseVertex(0);
                }
                else
                {
                    _args[0] = _args[1] = _args[2] = _args[3] = 0;
                }

                _cullingCounterBuffer.SetData(_cullingCounterArgs);
                _argsBuffer.SetData(_args);
                _cullInputCounterBuffer.SetData(_totalGrassCounter);

                _drawCS.SetFloat(MagicGrassProperty._GrassDensity, _grassDensity);
                _drawCS.SetFloat(MagicGrassProperty._MapSize, (float)_mapSize);
                _drawCS.SetFloat(MagicGrassProperty._TerrainSize, terrain.terrainData.size.x);
                _drawCS.SetVector(MagicGrassProperty._TerrainPivotWS, terrain.transform.position);

                _frustumCullingCS.SetBool(MagicGrassProperty._EnabledCull, true);
                _frustumCullingCS.SetFloat(MagicGrassProperty._MaxInstanceCount, _grassDensity);
                _frustumCullingCS.SetVector(MagicGrassProperty._HiZTextureSize, new Vector2(1024, 1024));
                _frustumCullingCS.SetBuffer(0, MagicGrassProperty._InputCounterBuffer, _cullInputCounterBuffer);
                _cellBounds = GetCellCenter();
                _runtimeMaterial.SetFloat(MagicGrassProperty._TerrainSize, terrain.terrainData.size.x);
            }

            public void LightweightInit()
            {
                _culledResultBuffer = new ComputeBuffer(ComputeBufferSize, sizeof(float) * 3 * 4,
                    ComputeBufferType.Append);
                _drawedGrassBuferr = new ComputeBuffer(ComputeBufferSize, sizeof(float) * 3 * 4,
                    ComputeBufferType.Append);

                _drawCS.SetBuffer(0, MagicGrassProperty._DrawGrassResult, _drawedGrassBuferr);
                _frustumCullingCS.SetBuffer(0, MagicGrassProperty._CullInputs, _drawedGrassBuferr);
                _frustumCullingCS.SetBuffer(0, MagicGrassProperty._Lod0CullResult, _culledResultBuffer);
                _runtimeMaterial.SetBuffer("_MeshDatas", _culledResultBuffer);
            }

            public void ClearCachedBuffer()
            {
                if (_drawedGrassBuferr != null && _drawedGrassBuferr.IsValid())
                    _drawedGrassBuferr.Dispose();
                if (_culledResultBuffer != null && _culledResultBuffer.IsValid())
                    _culledResultBuffer.Dispose();
            }

            public void Update(MagicGrassSettingAsset magicGrassSettingAsset, Vector3 cameraPosition,
                float spawnExcludeThreshold, float spawnIncludeThreshold, RenderTexture hizDepthTexture,
                InteractMotionBufferController interactController, bool syncLodMaterial)
            {
#if UNITY_EDITOR
                s_markGrassSyncLod.Begin();
                if (syncLodMaterial)
                {
                    //only sync non-texture properties so the runtime material won't act funny
                    foreach (var prop in _cachedProperties)
                    {
                        if (prop.type == MaterialProperty.PropType.Float ||
                            prop.type == MaterialProperty.PropType.Range)
                        {
                            _runtimeMaterial.SetFloat(prop.name, _sourceMaterial.GetFloat(prop.name));
                        }

                        if (prop.type == MaterialProperty.PropType.Color)
                        {
                            _runtimeMaterial.SetColor(prop.name, _sourceMaterial.GetColor(prop.name));
                        }

                        if (prop.type == MaterialProperty.PropType.Vector)
                        {
                            _runtimeMaterial.SetVector(prop.name, _sourceMaterial.GetVector(prop.name));
                        }
                    }

                    _runtimeMaterial.SetBuffer("_MeshDatas", _culledResultBuffer);
                }

                s_markGrassSyncLod.End();
#endif
                s_markGrassRendererRender1.Begin();

                var threadSize = (float)_cellSize;
                _drawedGrassBuferr.SetCounterValue(0);
                _drawCS.SetVector(MagicGrassProperty._CameraPositionWS, cameraPosition);
                _drawCS.SetFloat(MagicGrassProperty._ExcludeThreshold, spawnExcludeThreshold);
                _drawCS.SetFloat(MagicGrassProperty._IncludeThreshold, spawnIncludeThreshold);
                _drawCS.SetFloat(MagicGrassProperty._CloseDistance, magicGrassSettingAsset.CloseDistance);
                _drawCS.SetFloat(MagicGrassProperty._MidDistance, magicGrassSettingAsset.MidDistance);
                _drawCS.SetFloat(MagicGrassProperty._FarDistance, magicGrassSettingAsset.FarDistance);
                s_markGrassRendererRender1.End();

                _drawCS.Dispatch(0, Mathf.CeilToInt(threadSize / 8f), Mathf.CeilToInt(threadSize / 8f), 1);
                _culledResultBuffer.SetCounterValue(0);
                _cullInputCounterBuffer.SetCounterValue(0);
                ComputeBuffer.CopyCount(_drawedGrassBuferr, _cullInputCounterBuffer, 0);

                s_markGrassRendererRender2.Begin();

                _frustumCullingCS.SetVector(MagicGrassProperty._CameraPositionWS, cameraPosition);
                _frustumCullingCS.SetFloat(MagicGrassProperty._FarDistance, magicGrassSettingAsset.FarDistance);
                if (_hizBuffer == null)
                {
                    _hizBuffer = hizDepthTexture;
                    _frustumCullingCS.SetTexture(0, MagicGrassProperty._HiZMap, _hizBuffer);
                    _frustumCullingCS.SetFloat("_IgnoreHiz", Application.isPlaying ? 0 : 1f);
                }

                s_markGrassRendererRender2.End();

                _frustumCullingCS.Dispatch(0, Mathf.CeilToInt(threadSize / 8f), Mathf.CeilToInt(threadSize / 8f),
                    _grassDensity);
                //ComputeBuffer.CopyCount(m_drawedGrassBuferr, m_cullingCounterBuffer, sizeof(uint));
                //m_cullingCounterBuffer.GetData(m_cullingCounterArgs);
                // Debug.LogError("count = " + m_cullingCounterArgs[1]);
                s_markGrassRendererRender3.Begin();
                ComputeBuffer.CopyCount(_culledResultBuffer, _argsBuffer, sizeof(uint));
                // m_highLodMat.SetVector(MagicGrassProperty._MyMainLightDirection, mainLight.transform.forward);
                _runtimeMaterial.SetTexture(MagicGrassProperty._GroundTintMap, _groundTintMap);
                _runtimeMaterial.SetTexture(MagicGrassProperty._InteractMap, interactController.InteractBuffer);
                _runtimeMaterial.SetVector(MagicGrassProperty._InteractMotionCenterWS,
                    interactController.transform.position);
                _runtimeMaterial.SetFloat(MagicGrassProperty._InteractMapSize, interactController.MapSize);
                _runtimeMaterial.SetFloat(MagicGrassProperty._CloseDistance, magicGrassSettingAsset.CloseDistance);
                _runtimeMaterial.SetFloat(MagicGrassProperty._MidDistance, magicGrassSettingAsset.MidDistance);
                _runtimeMaterial.SetFloat(MagicGrassProperty._FarDistance, magicGrassSettingAsset.FarDistance);
                _runtimeMaterial.SetFloat(MagicGrassProperty._MaxDinstance, magicGrassSettingAsset.FarDistance);
                _runtimeMaterial.SetFloat(MagicGrassProperty._FadeInDistance,
                    magicGrassSettingAsset.MidDistance - magicGrassSettingAsset.CloseDistance);
                Graphics.DrawMeshInstancedIndirect(_mesh, 0, _runtimeMaterial,
                    new Bounds(cameraPosition, new Vector3(500, 500, 500)), _argsBuffer, 0, null,
                    UnityEngine.Rendering.ShadowCastingMode.Off, true);
                s_markGrassRendererRender3.End();
            }

            public void Clear()
            {
                if (_cullInputCounterBuffer != null && _cullInputCounterBuffer.IsValid())
                    _cullInputCounterBuffer.Dispose();
                if (_culledResultBuffer != null && _culledResultBuffer.IsValid())
                    _culledResultBuffer.Dispose();
                if (_drawedGrassBuferr != null && _drawedGrassBuferr.IsValid())
                    _drawedGrassBuferr.Dispose();
                if (_argsBuffer != null && _argsBuffer.IsValid())
                    _argsBuffer.Dispose();
                if (_cullingCounterBuffer != null && _cullingCounterBuffer.IsValid())
                    _cullingCounterBuffer.Dispose();

                if (Application.isPlaying)
                {
                    Destroy(_frustumCullingCS);
                    Destroy(_drawCS);
                    Destroy(_runtimeMaterial);
                }
                else
                {
                    DestroyImmediate(_frustumCullingCS);
                    DestroyImmediate(_drawCS);
                    DestroyImmediate(_runtimeMaterial);
                }
            }
        }

        public ComputeShader DrawGrassCS;
        public ComputeShader FrustumCullingCS;

        [FormerlySerializedAs("m_magicGrassSettingAsset")] [ReadOnly, SerializeField]
        private MagicGrassSettingAsset _magicGrassSettingAsset;

        [FormerlySerializedAs("m_iteractMotionController")] [ReadOnly, SerializeField]
        private InteractMotionBufferController _iteractMotionController;

        private PaintTextureController _paintTextureController;

        [FormerlySerializedAs("m_magicGrassRenderModels")] [SerializeField]
        private List<MagicGrassRenderModel> _magicGrassRenderModels = new List<MagicGrassRenderModel>();

        private HizBufferController _hizBufferController;
        private RuntimeMapsController _runtimeMapsController;
        private Terrain _targetTerrain;
        private RenderTexture _terrainHeightMap;
        private Texture2D[] _paintTextures;
        private RenderTexture _hizTextureEditorModeReplaceTexture;
        private bool _needSyncLodMaterial;
        private Camera _gameMainCamera;

        private Camera _editorCamera;

        //all grass renderer related arrays
        private MassGrassRenderer[] _renderers;
        private Bounds[] _cellBounds;
        private bool[] _cellStatuses;
        private bool[] _requestLightInitBuffers;
        private bool[] _requestClearCachedBuffers;
        private int[] _rendererModelndexes;
        private CullingGroup _cullingGroup;
        private bool _initProcessed;
        private bool _editorUpdateThisFrame;
        private float[] _distanceBands = new float[] { 10, 50, 150 };

        public RenderTexture HizTexture
        {
            get
            {
                if (_hizBufferController.Texture == null) return _hizTextureEditorModeReplaceTexture;
                return _hizBufferController.Texture;
            }
        }

        public RuntimeMapsController RuntimeMapsController => _runtimeMapsController;
        public RenderTexture SurfaceTexture => _runtimeMapsController.SurfaceColorRT;
        public RenderTexture NormalTexture => _runtimeMapsController.NormalRT;
        public RenderTexture HeightTexture => _targetTerrain.terrainData.heightmapTexture;

        //NeedSyncMaterial is trigger from inspector editor script
        public bool NeedSyncMaterial
        {
            get { return _needSyncLodMaterial; }
            set { _needSyncLodMaterial = value; }
        }

        public List<MagicGrassRenderModel> Models => _magicGrassRenderModels;
        public Action OnForceRebuild;

        public void RemoveModelFromEditor()
        {
            Debug.Log("_magicGrassRenderModels count = " + _magicGrassRenderModels.Count);
        }

        public void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.update += OnEditorUpdate;
#endif
            _initProcessed = false;
            DrawGrassCS = Resources.Load<ComputeShader>("IndirectDetailFetching");
            FrustumCullingCS = Resources.Load<ComputeShader>("FrustrumCulling");
            _magicGrassSettingAsset = Resources.Load<MagicGrassSettingAsset>("MagicGrassSettingAsset");
            Shader.SetGlobalInt("_IsGammaColorSpace", QualitySettings.activeColorSpace == ColorSpace.Gamma ? 1 : 0);
            if (Application.isEditor && !Application.isPlaying)
            {
                if (_hizTextureEditorModeReplaceTexture != null)
                {
                    _hizTextureEditorModeReplaceTexture.Release();
                }

                _hizTextureEditorModeReplaceTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.RGHalf);
            }

            _gameMainCamera = Camera.main;

            if (_gameMainCamera.GetComponent<HizBufferController>() == null)
            {
                _gameMainCamera.gameObject.AddComponent<HizBufferController>();
            }

            _hizBufferController = _gameMainCamera.gameObject.GetComponent<HizBufferController>();
            _runtimeMapsController = GetComponent<RuntimeMapsController>();
            _runtimeMapsController.CreateSurfaceNormal();
            _runtimeMapsController.CreateSurfaceColor();
            _targetTerrain = GetComponent<Terrain>();
            _terrainHeightMap = _targetTerrain.terrainData.heightmapTexture;

            _iteractMotionController = GetComponent<InteractMotionBufferController>();
            _iteractMotionController.Init(_magicGrassSettingAsset, _targetTerrain.terrainData.size.x,
                _targetTerrain.terrainData.size.y, _targetTerrain.terrainData.heightmapTexture);
            _paintTextureController = GetComponent<PaintTextureController>();
            if (_paintTextureController.PrepareRuntimeRT())
            {
                _paintTextures = GetComponent<PaintTextureController>().SourceTextures;
            }
            else
            {
                Debug.LogWarning(
                    "paint texture controller's source texture seems null, The renderer will skip to prevent more error.");
                _initProcessed = false;
                return;
            }

            InitRenderers();
            _initProcessed = true;
        }

        private void InitRenderers()
        {
            var subDividedRenderers = new List<MassGrassRenderer>();
            var rendererModelIndexes = new List<int>();

            var index = 0;
            foreach (var model in _magicGrassRenderModels)
            {
                //if one of these properties is null, don't init the model.
                if (model == null || model.Mesh == null || model.InstanceMaterial == null || model.GrassDensity < 1)
                {
                    index++;
                    continue;
                }

                model.Init(index, _targetTerrain, _paintTextures, _terrainHeightMap,
                    _runtimeMapsController.NormalRT,
                    _runtimeMapsController.SurfaceColorRT, DrawGrassCS, FrustumCullingCS,
                    out _renderers);
                rendererModelIndexes.AddRange(Enumerable.Repeat(index, _renderers.Length).ToArray());
                subDividedRenderers.AddRange(_renderers);
                index++;
            }

            _renderers = new MassGrassRenderer[subDividedRenderers.Count];
            _rendererModelndexes = new int[subDividedRenderers.Count];
            _cellBounds = new Bounds[subDividedRenderers.Count];
            _cellStatuses = new bool[subDividedRenderers.Count];
            _requestLightInitBuffers = new bool[subDividedRenderers.Count];
            _requestClearCachedBuffers = new bool[subDividedRenderers.Count];

            var rendererBoundingSpheres = new BoundingSphere[subDividedRenderers.Count];

            for (int i = 0; i < subDividedRenderers.Count(); i++)
            {
                _renderers[i] = subDividedRenderers[i];
                _cellBounds[i] = subDividedRenderers[i].CellBounds;
                _rendererModelndexes[i] = rendererModelIndexes[i];
                rendererBoundingSpheres[i] = new BoundingSphere(subDividedRenderers[i].CellBounds.center,
                    subDividedRenderers[i].CellBounds.extents.x * 1.41f);
            }

            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = _gameMainCamera;
            _cullingGroup.SetBoundingSpheres(rendererBoundingSpheres);
            _cullingGroup.SetDistanceReferencePoint(_gameMainCamera.transform);
            _cullingGroup.SetBoundingDistances(_distanceBands);
            _cullingGroup.onStateChanged = OnCullingStateChange;
            subDividedRenderers.Clear();
            rendererModelIndexes.Clear();
        }

        public void ForceRebuildRenderers()
        {
            if (_initProcessed)
            {
                OnDisable();
            }

            OnEnable();
            OnForceRebuild?.Invoke();
        }

        //ugly....
        private void OnCullingStateChange(CullingGroupEvent cullingGroupEvent)
        {
            _cellStatuses[cullingGroupEvent.index] = cullingGroupEvent.isVisible;
            //if the cell is visible, and it has no cached, we need to update the buffer to render the cell.
            if (cullingGroupEvent.currentDistance <= 2)
            {
                _requestLightInitBuffers[cullingGroupEvent.index] = cullingGroupEvent.isVisible &&
                                                                     !_renderers[cullingGroupEvent.index].HasCached;
            }
            //if the cell is invisible, and it's been cache at the moment, we need to update the buffer to clear the cell.
            else
            {
                _requestClearCachedBuffers[cullingGroupEvent.index] = !cullingGroupEvent.isVisible &&
                                                                       _renderers[cullingGroupEvent.index].HasCached;
            }
        }

        public void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif
            if (_cullingGroup != null)
            {
                _cullingGroup.Dispose();
            }

            if (_hizTextureEditorModeReplaceTexture != null)
            {
                _hizTextureEditorModeReplaceTexture.Release();
            }

            if (_renderers != null)
            {
                foreach (var renderer in _renderers)
                {
                    renderer.Clear();
                }
            }

            foreach (var model in _magicGrassRenderModels)
            {
                if (model == null)
                    continue;
                model.Disable();
            }

            if (_iteractMotionController != null)
                _iteractMotionController.Clear();
        }

        private Matrix4x4 _lastProjectionMatrix;
        private Matrix4x4 _lastViewMatrix;

#if UNITY_EDITOR

        private float _editorupdateInterval = 1 / 15f; // 30 fps update interval
        private float _editorupdateTimeSinceLastUpdate = 0.0f;
        private Vector3 _lastSceneCamePos;
        private Quaternion _lastSceneCameraRot;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                if (SceneView.lastActiveSceneView.camera != null)
                {
                    var nowPos = SceneView.lastActiveSceneView.camera.transform.position;
                    var nowRot = SceneView.lastActiveSceneView.camera.transform.rotation;
                    if ((_lastSceneCamePos - nowPos).sqrMagnitude > 0.01f || _lastSceneCameraRot != nowRot)
                    {
                        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                        UnityEditor.SceneView.RepaintAll();
                    }

                    _lastSceneCameraRot = nowRot;
                    _lastSceneCamePos = nowPos;
                }
            }
        }

        private void OnEditorUpdate()
        {
            _editorupdateTimeSinceLastUpdate += Time.deltaTime;
            if (_editorupdateTimeSinceLastUpdate < _editorupdateInterval)
            {
                return;
            }
            else
            {
                _editorupdateTimeSinceLastUpdate -= _editorupdateInterval;
            }

            if (Application.isPlaying || _editorUpdateThisFrame || _gameMainCamera == null || !_initProcessed)
                return;
            _needSyncLodMaterial = true;

            if (UnityEditor.EditorWindow.focusedWindow != null &&
                UnityEditor.EditorWindow.focusedWindow.ToString().Contains("Scene") &&
                SceneView.lastActiveSceneView != null)
            {
                _editorCamera = SceneView.lastActiveSceneView.camera;
                _cullingGroup.targetCamera = _editorCamera;
                _cullingGroup.SetDistanceReferencePoint(_editorCamera.transform);
            }

            if (UnityEditor.EditorWindow.focusedWindow != null &&
                UnityEditor.EditorWindow.focusedWindow.ToString().Contains("GameView"))
            {
                _editorCamera = null;
                if (_cullingGroup.targetCamera != _gameMainCamera)
                {
                    _cullingGroup.targetCamera = _gameMainCamera;
                    _cullingGroup.SetDistanceReferencePoint(_gameMainCamera.transform);
                }
            }

            UpdateCameraMatrix();
        }

#endif

        private void UpdateCameraMatrix()
        {
            _lastProjectionMatrix = _gameMainCamera.projectionMatrix;
            _lastViewMatrix = _gameMainCamera.worldToCameraMatrix;

            if (!Application.isPlaying && _editorCamera != null)
            {
                _lastProjectionMatrix = _editorCamera.projectionMatrix;
                _lastViewMatrix = _editorCamera.worldToCameraMatrix;
            }

            var vpMatrix = _lastProjectionMatrix * _lastViewMatrix;
            Shader.SetGlobalMatrix(MagicGrassProperty._MatrixVP, vpMatrix);
        }

        private Vector3 GetCameraPosition()
        {
            if (!Application.isPlaying && _editorCamera != null)
            {
                return _editorCamera.transform.position;
            }

            return _gameMainCamera.transform.position;
        }

        private void Update()
        {
            s_markGrassUpdate.Begin();

            if (_gameMainCamera == null || !_initProcessed)
                return;
            _editorUpdateThisFrame = true;

            if (Application.isPlaying && _hizBufferController.Texture == null)
                return;

            UpdateCameraMatrix();
            var cameraPosition = GetCameraPosition();

            for (int i = 0; i < _renderers.Length; i++)
            {
                var model = _magicGrassRenderModels[_rendererModelndexes[i]];
                if (_requestLightInitBuffers[i])
                {
                    
                    _renderers[i].LightweightInit();
                    _requestLightInitBuffers[i] = false;
                }

                if (_requestClearCachedBuffers[i])
                {
                    _renderers[i].ClearCachedBuffer();
                    _requestClearCachedBuffers[i] = false;
                }

                if (_cellStatuses[i])
                {
                    var hizDepthTexture = Application.isPlaying
                        ? _hizBufferController.Texture
                        : _hizTextureEditorModeReplaceTexture;
                    _renderers[i].Update(_magicGrassSettingAsset, cameraPosition, model.SpawnExcludeThreshold,
                        model.SpawnIncludeThreshold, hizDepthTexture, _iteractMotionController, _needSyncLodMaterial);
                }
            }

            _needSyncLodMaterial = false;
            _editorUpdateThisFrame = false;
            s_markGrassUpdate.End();
        }
    }
}