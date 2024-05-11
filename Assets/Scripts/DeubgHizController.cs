using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

[ExecuteInEditMode]
public class DeubgHizController : MonoBehaviour
{
    public string FetchTextureName;
    private RawImage _image;
    public int HiZLevel = 0;
    private void Init()
    {
        var uiImage = GetComponent<RawImage>();
        if (uiImage != null && _image != uiImage)
        {
            _image = uiImage;
        }

        _image.material = new Material(Shader.Find("IndirectRendering/HiZ/DebugHiz"));
        _image.material.SetInt("_LOD", HiZLevel);
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (FetchTextureName != string.Empty)
        {
            var texture = Shader.GetGlobalTexture(FetchTextureName);
            _image.texture = texture;
            _image.material.SetTexture("_MainTex", texture);
            _image.material.SetFloat("_LOD", HiZLevel);
        }
    }
}
