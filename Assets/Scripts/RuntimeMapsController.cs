using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class RuntimeMapsController : MonoBehaviour
{
    private RenderTexture _normalTexture;
    [SerializeField]
    private RenderTexture _surfaceColorTexture;

    public RenderTexture NormalRT => _normalTexture;
    public RenderTexture SurfaceColorRT => _surfaceColorTexture;

    public void CreateSurfaceNormal()
    {
        if (_normalTexture != null && _normalTexture.IsCreated())
        {
            _normalTexture.Release();
        }
        var terrainData = GetComponent<Terrain>().terrainData;
        var mat = new Material(Shader.Find("Unlit/NormalGen"));
        mat.SetFloat("_TerrainHeight", terrainData.size.y);
        mat.SetFloat("_TerrainWidthLength", terrainData.size.x);
        _normalTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);

        RenderTexture.active = _normalTexture;
        Graphics.Blit(terrainData.heightmapTexture, _normalTexture, mat);
        RenderTexture.active = null;
    }

    public void CreateSurfaceColor()
    {
        if (_surfaceColorTexture != null && _surfaceColorTexture.IsCreated())
        {
            _surfaceColorTexture.Release();
        }
        var terrainData = GetComponent<Terrain>().terrainData;
        var mat = new Material(Shader.Find("Hidden/TerrainSurfaceColor"));
        _surfaceColorTexture = new RenderTexture(terrainData.alphamapWidth, terrainData.alphamapHeight, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);

        mat.SetTexture("_SplatMap", terrainData.alphamapTextures[0]);
        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            if (i >= 4)
                break;
            mat.SetTexture("_Diffuse" + i, terrainData.terrainLayers[i].diffuseTexture);
            mat.SetVector("_ST" + i, new Vector4(terrainData.size.x / terrainData.terrainLayers[i].tileSize.x,
            terrainData.size.y / terrainData.terrainLayers[i].tileSize.y, 0, 0));
        }
        RenderTexture.active = _surfaceColorTexture;
        Graphics.Blit(null, _surfaceColorTexture, mat);
        RenderTexture.active = null;
    }

    private void OnDestroy()
    {
        if (_normalTexture != null && _normalTexture.IsCreated())
            _normalTexture.Release();

        if (_surfaceColorTexture != null && _surfaceColorTexture.IsCreated())
            _surfaceColorTexture.Release();
    }
}