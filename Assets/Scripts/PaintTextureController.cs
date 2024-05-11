using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace MagicGrass
{
    [ExecuteInEditMode]
    public class PaintTextureController : MonoBehaviour
    {
        private const int RT_SIZE = 512;
        private const string MAGIC_GRASS_PREFIX = "Magic_Grass_Detail";
        private int _currentPaintTextureIndex = 0;

        //Runtime RTs
        [ReadOnly, SerializeField] private RenderTexture[] _paintTextures;

        //Texture assets that stored along terrain asset
        [ReadOnly, SerializeField] private Texture2D[] _sourceTextures;

        public RenderTexture[] RunTimePaintTexture => _paintTextures;

        public Texture2D[] SourceTextures
        {
            get
            {
#if UNITY_EDITOR
                if (!IsSourceTextureExist())
                {
                    CreateSourceTextures();
                }
#endif
                return _sourceTextures;
            }
        }

        public int PaintTextureIndex => _currentPaintTextureIndex;
        private const int MAX_SPLAT_MAP_COUNT = 2;

        public void SetCurrentSplatMap(int index)
        {
            _currentPaintTextureIndex = index;
        }

        private void InitRT()
        {
            _paintTextures = new RenderTexture[SourceTextures.Length];
            for (int i = 0; i < SourceTextures.Length; i++)
            {
                _paintTextures[i] = new RenderTexture(RT_SIZE, RT_SIZE, 0, RenderTextureFormat.ARGB32);
                CopyFromTexture2D(SourceTextures[i], _paintTextures[i]);
            }
        }

        public bool PrepareRuntimeRT()
        {
            InitRT();
            if (_sourceTextures == null)
            {
                return false;
            }

            return true;
        }

        private void CopyFromTexture2D(Texture2D texture2d, RenderTexture rt)
        {
            RenderTexture.active = rt;
            Graphics.Blit(texture2d, rt);
            RenderTexture.active = null;
        }

        public void WriteRtToTexture2D(RenderTexture rt, Texture2D texture2d)
        {
            RenderTexture.active = rt;
            texture2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture2d.Apply();
            RenderTexture.active = null;
        }

        private void OnDestroy()
        {
            if (_paintTextures != null && _paintTextures.Length > 0)
            {
                foreach (var rt in _paintTextures)
                {
                    if (rt != null && rt.IsCreated())
                        rt.Release();
                }
            }
        }

#if UNITY_EDITOR

        private void LoadSourceTextures()
        {
            var terrainDataAssets =
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(GetComponent<Terrain>().terrainData));
            var textures = terrainDataAssets.Where(x => x.name.Contains(MAGIC_GRASS_PREFIX)).OrderBy(x => x.name)
                .ToList();
            _sourceTextures = new Texture2D[2];
            for (int i = 0; i < textures.Count(); i++)
            {
                _sourceTextures[i] = textures[i] as Texture2D;
            }
        }

        private bool IsSourceTextureExist()
        {
            for (int i = 0; i < MAX_SPLAT_MAP_COUNT; i++)
            {
                //check if sourceTexture exist, create textures if not.
                if (!HasSourceTextureFromIndex(i))
                {
                    return false;
                }
            }

            return true;
        }

        private void CreateSourceTextures()
        {
            for (int i = 0; i < MAX_SPLAT_MAP_COUNT; i++)
            {
                //check if sourceTexture exist, create textures if not.
                if (!HasSourceTextureFromIndex(i))
                {
                    CreateTexture2DITerrainData(GetComponent<Terrain>(), i);
                }
            }
        }

        private void OnEnable()
        {
            CreateSourceTextures();
        }

        public bool HasSourceTextureFromIndex(int index)
        {
            var terrainDataAssets =
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(GetComponent<Terrain>().terrainData));
            return terrainDataAssets.Any(x => x.name == MAGIC_GRASS_PREFIX + "_" + index);
        }

        public bool IsTextureArrayNeedUpdate(int splatMapIndex)
        {
            return RunTimePaintTexture == null || _sourceTextures == null ||
                   RunTimePaintTexture.Length == 0 || _sourceTextures.Length == 0 ||
                   _sourceTextures[splatMapIndex] == null ||
                   splatMapIndex != PaintTextureIndex;
        }

        private void OnValidate()
        {
            if (_sourceTextures == null || _sourceTextures.Length == 0 ||
                _sourceTextures[_currentPaintTextureIndex] == null)
            {
                var guid = AssetDatabase.GUIDFromAssetPath(
                    AssetDatabase.GetAssetPath(GetComponent<Terrain>().terrainData));
                var terrainDataAssets =
                    AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(GetComponent<Terrain>().terrainData));
                var textures = terrainDataAssets.Where(x => x.name.Contains(MAGIC_GRASS_PREFIX)).ToList();
                if (textures.Count > 0)
                {
                    LoadSourceTextures();
                }
                else
                {
                    for (int i = 0; i < MAX_SPLAT_MAP_COUNT; i++)
                    {
                        //check if sourceTexture exist, create textures if not.
                        if (!HasSourceTextureFromIndex(i))
                        {
                            CreateTexture2DITerrainData(GetComponent<Terrain>(), i);
                        }
                    }
                }
            }
        }

        public Texture2D CreateTexture2DITerrainData(Terrain terrain, int index)
        {
            var t2d = new Texture2D(RT_SIZE, RT_SIZE, TextureFormat.ARGB32, false);
            var colors = new Color[RT_SIZE * RT_SIZE];
            t2d.SetPixels(0, 0, t2d.width, t2d.width, colors);
            t2d.Apply();
            t2d.name = MAGIC_GRASS_PREFIX + "_" + index;
            AssetDatabase.AddObjectToAsset(t2d, terrain.terrainData);
            var path = AssetDatabase.GetAssetPath(t2d);
            t2d.wrapMode = TextureWrapMode.Clamp;
            AssetDatabase.SaveAssets();

            LoadSourceTextures();
            return t2d;
        }


        [ContextMenu("clean related assets")]
        public void ClearnRelatedAssets()
        {
            var terrainDataAssets =
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(GetComponent<Terrain>().terrainData));
            var assetList = new List<UnityEngine.Object>();
            foreach (var asset in terrainDataAssets)
            {
                if (asset.name.Contains("Detail Map") || asset.name.Contains(MAGIC_GRASS_PREFIX))
                {
                    assetList.Add(asset);
                }
            }

            foreach (var asset in assetList)
            {
                DestroyImmediate(asset, true);
            }

            CreateTexture2DITerrainData(GetComponent<Terrain>(), 0);
            AssetDatabase.SaveAssets();
        }

#endif
    }
}