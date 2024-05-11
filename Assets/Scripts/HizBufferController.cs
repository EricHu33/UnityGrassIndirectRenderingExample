// https://github.com/gokselgoktas/hi-z-buffer

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class HizBufferController : MonoBehaviour
{
    #region Variables
    // Private 
    private Camera _mainCamera = null;
    private int _lODCount = 0;
    private CameraEvent _cameraEvent = CameraEvent.AfterReflections;
    private Vector2 _textureSize;
    private Material _generateBufferMaterial = null;
    private Material _debugMaterial = null;
    private CameraEvent _lastCameraEvent = CameraEvent.AfterReflections;
    private RenderTexture _shadowmapCopy;
    private RenderTargetIdentifier _shadowmap;
    private CommandBuffer _lightShadowCommandBuffer;

    [FormerlySerializedAs("m_HiZDepthTexture")] [SerializeField]
    private RenderTexture _hiZDepthTexture = null;

    // Public Properties
    public int DebugLodLevel { get; set; }
    public Vector2 TextureSize { get { return _textureSize; } }
    public RenderTexture Texture { get { return _hiZDepthTexture; } }

    // Consts
    private const int MAXIMUM_BUFFER_SIZE = 1024;

    // Enums
    private enum Pass
    {
        Blit,
        Reduce
    }

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        RenderPipelineManager.endContextRendering += OnBeginContextRendering;
        _mainCamera = Camera.main;
        _generateBufferMaterial = new Material(Shader.Find("IndirectRendering/HiZ/Buffer"));
        _debugMaterial = new Material(Shader.Find("IndirectRendering/HiZ/DebugHiz"));
        _mainCamera.depthTextureMode = DepthTextureMode.Depth;
        int size = (int)Mathf.Max((float)_mainCamera.pixelWidth, (float)_mainCamera.pixelHeight);
        size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
    }
    private void OnDisable()
    {
        if (_hiZDepthTexture != null)
        {
            _hiZDepthTexture.Release();
            _hiZDepthTexture = null;
        }
        RenderPipelineManager.endContextRendering -= OnBeginContextRendering;
    }

    public void InitializeTexture()
    {
        if (_hiZDepthTexture != null)
        {
            _hiZDepthTexture.Release();
        }

        int size = (int)Mathf.Max((float)_mainCamera.pixelWidth, (float)_mainCamera.pixelHeight);
        size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
        _textureSize.x = size;
        _textureSize.y = size;
        _hiZDepthTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        _hiZDepthTexture.filterMode = FilterMode.Point;
        _hiZDepthTexture.useMipMap = true;
        _hiZDepthTexture.autoGenerateMips = false;
        _hiZDepthTexture.Create();
        _hiZDepthTexture.hideFlags = HideFlags.DontSave;
    }

    private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        int size = (int)Mathf.Max((float)_mainCamera.pixelWidth, (float)_mainCamera.pixelHeight);
        size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
        _textureSize.x = size;
        _textureSize.y = size;
        _lODCount = (int)Mathf.Floor(Mathf.Log(size, 2f));

        if (_lODCount == 0)
        {
            return;
        }

        if (_hiZDepthTexture == null
            || (_hiZDepthTexture.width != size
            || _hiZDepthTexture.height != size)
            || _lastCameraEvent != _cameraEvent
            )
        {
            InitializeTexture();

            _lastCameraEvent = _cameraEvent;
        }
        var prevRT1 = RenderTexture.active;
        var rt1 = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.RGHalf);
        RenderTexture.active = rt1;
        Graphics.Blit(rt1, _hiZDepthTexture, _generateBufferMaterial, (int)Pass.Blit);
        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.active = prevRT1;
        RenderTexture rt;
        RenderTexture lastRt = null;
        for (int i = 0; i < _lODCount; ++i)
        {
            size >>= 1;
            size = Mathf.Max(size, 1);
            rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.RGHalf);
            rt.filterMode = FilterMode.Point;
            var prevRT = RenderTexture.active;
            RenderTexture.active = rt;
            if (i == 0)
            {
                Graphics.Blit(null, rt);
                Graphics.Blit(_hiZDepthTexture, rt, _generateBufferMaterial, (int)Pass.Reduce);
            }
            else
            {
                Graphics.Blit(null, rt);
                Graphics.Blit(lastRt, rt, _generateBufferMaterial, (int)Pass.Reduce);
            }

            lastRt = rt;
            Graphics.CopyTexture(rt, 0, 0, _hiZDepthTexture, 0, i + 1);
            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = prevRT;
        }

        _debugMaterial.SetInt("_NUM", 1);
        _debugMaterial.SetInt("_LOD", 0);
        Shader.SetGlobalTexture("_hiZDepthTexture", _hiZDepthTexture);
    }



    #endregion
}