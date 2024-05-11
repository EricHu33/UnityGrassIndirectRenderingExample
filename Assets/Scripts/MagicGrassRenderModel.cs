using System.Collections.Generic;
using UnityEngine;


namespace MagicGrass
{
    [CreateAssetMenu(menuName = "MagicGrass/MagicGrassRenderModel")]
    public class MagicGrassRenderModel : ScriptableObject
    {
        [SerializeField]
        public MagicGrassGrassMapSize MapSize = MagicGrassGrassMapSize.Mid;
        public int RandomSeed;
        public Mesh Mesh;
        public Material InstanceMaterial;
        public TerrainLayer[] ExcludeLayers;
        [Range(0f, 2f)]
        public float SpawnExcludeThreshold = 1.0f;
        public TerrainLayer[] IncludeLayers;
        [Range(0f, 2f)]
        public float SpawnIncludeThreshold = 1.0f;
        [Range(1, 16)]
        public int GrassDensity = 1;
        private int _cellSize = 1;
        private Texture2DArray _alphaMapArray;
        private const int CELL_COUNT_PER_EDGE = 4;

        public bool IsValidModel =>
            Mesh != null && InstanceMaterial != null && GrassDensity > 0 && _alphaMapArray != null;

        public MagicGrassRenderModel()
        {
            RandomSeed = 1;
            MapSize = MagicGrassGrassMapSize.Mid;
        }

        public void Init(int elementIndex, Terrain terrain, Texture2D[] detailMaps,
            RenderTexture heightMap, RenderTexture surfaceNormalMap, RenderTexture surfaceColorMap,
            ComputeShader drawCS, ComputeShader cullCS,
            out IndirectDrawController.MassGrassRenderer[] renderers)
        {
            //split compute shader cell into 2x2 grid, use frustum culling to pre-cull the cells before sending data to GPU 
            _cellSize = (int)MapSize / CELL_COUNT_PER_EDGE;

            var detailMapIndex = elementIndex;
            detailMapIndex = detailMapIndex < 4 ? 0 : 1;

            var excludeLayerIndexes = new HashSet<int>();
            var includeLayerIndexes = new HashSet<int>();
            var data = terrain.terrainData;
            var excludeLayersSet = new HashSet<TerrainLayer>(ExcludeLayers);
            var includeLayersSet = new HashSet<TerrainLayer>(IncludeLayers);
            for (int i = 0; i < data.terrainLayers.Length; i++)
            {
                var layer = data.terrainLayers[i];
                if (excludeLayersSet.Contains(layer) && !excludeLayerIndexes.Contains(i))
                {
                    excludeLayerIndexes.Add(i);
                }
                else if (includeLayersSet.Contains(layer) && !includeLayerIndexes.Contains(i))
                {
                    includeLayerIndexes.Add(i);
                }
            }

            //maybe it's possible to async init 2d array?
            _alphaMapArray = new Texture2DArray(terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight,
                terrain.terrainData.alphamapTextureCount, TextureFormat.RGBA32, false, false);
            for (int i = 0; i < terrain.terrainData.alphamapTextureCount; i++)
            {
                Graphics.CopyTexture(terrain.terrainData.alphamapTextures[i], 0, 0, _alphaMapArray, 0, 0);
            }

            _alphaMapArray.Apply();

            renderers = new IndirectDrawController.MassGrassRenderer[CELL_COUNT_PER_EDGE * CELL_COUNT_PER_EDGE];
            for (int x = 0; x < CELL_COUNT_PER_EDGE; x++)
            {
                for (int y = 0; y < CELL_COUNT_PER_EDGE; y++)
                {
                    var index = x * CELL_COUNT_PER_EDGE + y;
                    renderers[index] = new IndirectDrawController.MassGrassRenderer();
                    renderers[index].Init(_cellSize, x, y, MapSize, RandomSeed, terrain,
                        detailMaps, detailMapIndex, GetDetailSpaltChannels(elementIndex), heightMap, surfaceNormalMap,
                        surfaceColorMap, _alphaMapArray, excludeLayerIndexes, includeLayerIndexes, GrassDensity,
                        Mesh, InstanceMaterial, drawCS, cullCS);
                }
            }
        }

        public Vector4 GetDetailSpaltChannels(int ID)
        {
            var rChannel = ID == 0 ? 1f : 0f;
            var gChannel = ID == 1 ? 1f : 0f;
            var bChannel = ID == 2 ? 1f : 0f;
            var aChannel = ID == 3 ? 1f : 0f;

            var splatChannels = new Vector4(rChannel, gChannel, bChannel, aChannel);
            return splatChannels;
        }

        public void Disable()
        {
            if (Application.isPlaying)
            {
                Destroy(_alphaMapArray);
            }
            else
            {
                DestroyImmediate(_alphaMapArray);
            }
        }
    }
}