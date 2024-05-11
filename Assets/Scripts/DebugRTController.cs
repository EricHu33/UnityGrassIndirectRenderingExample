using System;
using System.Collections;
using System.Collections.Generic;
using MagicGrass;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

[ExecuteInEditMode]
public class DebugRTController : MonoBehaviour
{
    public enum RTType
    {
        InteractMap,
        SurfaceNormalMap,
        SurfaceColorMap,
        SplatMap0,
        SplatMap1,
    }

    public IndirectDrawController IndirectDrawController;
    public InteractMotionBufferController InteractMotionBufferController;
    [FormerlySerializedAs("_rtType")] public RTType RtType;
    public string FetchTextureName;
    private RawImage _image;
    private RenderTexture _rt;
    private void Init()
    {
        var uiImage = GetComponent<RawImage>();
        if (uiImage != null && _image != uiImage)
        {
            _image = uiImage;
        }
        if(_rt != null)
            _rt.Release();
        _rt = new RenderTexture(512, 512, 0, GraphicsFormat.R8G8_UNorm);
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
        
    }

    private void OnDisable()
    {
        if(_rt != null)
            _rt.Release();
    }

    // Update is called once per frame
    void Update()
    {
        if(IndirectDrawController == null || IndirectDrawController.RuntimeMapsController == null || InteractMotionBufferController == null)
            return;
        var originRT = RenderTexture.active;
        RenderTexture.active = _rt;
        switch(RtType)
        {
            case RTType.InteractMap:
                Graphics.Blit(InteractMotionBufferController.InteractBuffer, _rt);
                break;
            case RTType.SurfaceNormalMap:
                Graphics.Blit(IndirectDrawController.RuntimeMapsController.NormalRT, _rt);
                break;
            case RTType.SurfaceColorMap:
                Graphics.Blit(IndirectDrawController.RuntimeMapsController.SurfaceColorRT, _rt);
                break;
            case RTType.SplatMap0:
                Graphics.Blit(IndirectDrawController.GetComponent<PaintTextureController>().SourceTextures[0], _rt);
                break;
            case RTType.SplatMap1:
                if(IndirectDrawController.GetComponent<PaintTextureController>().SourceTextures.Length > 1)
                Graphics.Blit(IndirectDrawController.GetComponent<PaintTextureController>().SourceTextures[1], _rt);
                break;
        }
      //  _image.texture = _rt;
        _image.material.SetTexture("_BaseMap", _rt);
        _image.material.SetTexture("_MainTex", _rt);
        RenderTexture.active = originRT;
    }
}