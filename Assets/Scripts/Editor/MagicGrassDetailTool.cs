using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine.TerrainTools;
using UnityEditorInternal;
using UnityEngine.UIElements;

using MagicGrass;

internal class MagicGrassDetailTool : TerrainPaintTool<MagicGrassDetailTool>
{
    private float m_BrushRotation;
    private Material m_mat;
    private bool m_showMagicGrassDetail = true;
    private GUIContent m_title = new GUIContent();
    private Editor m_layerEditor;
    private VisualElement m_layerEditorRoot;
    private SerializedObject m_layersObject;
    private int m_currentSelectIndex = -1;
    private Texture2D m_brushTexture;

    // Name of the Terrain Tool. This appears in the tool UI.
    public override string GetName()
    {
        return "MagicGrassDetail/DetailPainter";
    }

    // Description for the Terrain Tool. This appears in the tool UI.
    public override string GetDescription()
    {
        return "This is a very basic Terrain Tool that doesn't do anything aside from appear in the list of Paint Terrain tools.";
    }
    private ReorderableList m_reordableList;

    public override void OnEnable()
    {
        base.OnEnable();
        m_initProcessed = false;
        m_brushTexture = Resources.Load<Texture2D>("magicgrass-brush");
    }
    private bool m_initProcessed = false;

    private bool[] buttonClicked = new bool[4];
    private string[] buttonNames = new string[4];
    private GUIStyle[] buttonStyle = new GUIStyle[4];
    public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
    {
        if (!m_initProcessed)
        {
            for (int i = 0; i < 4; i++)
            {
                buttonStyle[i] = new GUIStyle(GUI.skin.button);
            }
            m_initProcessed = true;
        }

        int textureRez = terrain.terrainData.heightmapResolution;
        editContext.ShowBrushesGUI(5, BrushGUIEditFlags.Opacity | BrushGUIEditFlags.Size, textureRez);

        GUI.skin.button.active.textColor = Color.red;
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        var normalBackground = new GUIStyle(GUI.skin.button).normal.background;
        var hoverBackground = new GUIStyle(GUI.skin.button).hover.background;
        for (int i = 0; i < 4; i++)
        {
            buttonStyle[i] = new GUIStyle(GUI.skin.button);


            if (i % 4 == 0)
            {
                buttonNames[i] = "R Channel";
            }
            if (i % 4 == 1)
            {
                buttonNames[i] = "G Channel";
            }
            if (i % 4 == 2)
            {
                buttonNames[i] = "B Channel";
            }
            if (i % 4 == 3)
            {
                buttonNames[i] = "A Channel";
            }


            var normalColor = GUI.backgroundColor;
            if (buttonClicked[i])
            {
                GUI.backgroundColor = Color.red;
                buttonStyle[i].normal.background = Texture2D.whiteTexture;
                buttonStyle[i].hover.background = Texture2D.whiteTexture;
            }
            else
            {
                buttonStyle[i].normal.background = normalBackground;
                buttonStyle[i].hover.background = hoverBackground;
            }


            if (GUILayout.Button(buttonNames[i], buttonStyle[i]))
            {
                m_currentSelectIndex = i;

                //reset other buttons
                for (int j = 0; j < 4; j++)
                {
                    if (j == i)
                    {
                        buttonClicked[j] = true;
                    }
                    else
                    {
                        buttonClicked[j] = false;
                    }
                }
            }
            GUI.backgroundColor = normalColor;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        m_title.text = "Detail Select";
        var selectionHint = m_currentSelectIndex == -1 ? "Please select a channel to paint" : "Selected Channel : " + buttonNames[m_currentSelectIndex];
        EditorGUILayout.LabelField(selectionHint);
    }

    public override void OnRenderBrushPreview(Terrain terrain, IOnSceneGUI editContext)
    {

        TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, m_brushTexture, editContext.brushSize);
    }

    public override bool OnPaint(Terrain terrain, IOnPaint editContext)
    {

        var rotationDegrees = 0.0f;
        var brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, rotationDegrees);

        var heightmapResolution = terrain.terrainData.heightmapResolution;
        var sharedBoundaryTexel = true;

        var paintContext = PaintContext.CreateFromBounds(terrain, brushXform.GetBrushXYBounds(), heightmapResolution, heightmapResolution,
                 0, sharedBoundaryTexel, true);
        if (m_mat == null)
            m_mat = new Material(Shader.Find("TerrainTool/CustomTerrainTool"));

        var targetAlpha = 1.0f;       // always 1.0 now -- no subtractive painting (we assume this in the ScatterAlphaMap)
        var brushStrength = Event.current.shift ? -editContext.brushStrength : editContext.brushStrength;
        var brushParams = new Vector4(brushStrength, targetAlpha, editContext.brushSize, 0.0f);

        var rChannel = m_currentSelectIndex % 4 == 0 ? 1f : 0f;
        var gChannel = m_currentSelectIndex % 4 == 1 ? 1f : 0f;
        var bChannel = m_currentSelectIndex % 4 == 2 ? 1f : 0f;
        var aChannel = m_currentSelectIndex % 4 == 3 ? 1f : 0f;

        var splatChannels = new Vector4(rChannel, gChannel, bChannel, aChannel);
        m_mat.SetTexture("_BrushTex", editContext.brushTexture);
        m_mat.SetVector("_BrushParams", brushParams);
        m_mat.SetVector("_SplatChannels", splatChannels);
        m_mat.SetFloat("_TerrainSize", terrain.terrainData.size.x);
        m_mat.SetVector("_CursorPosInTerrainUV", editContext.uv);
        var paintCtrl = terrain.GetComponent<PaintTextureController>();
        var splatMapIndex = m_currentSelectIndex <= 3 ? 0 : 1;
        paintCtrl.SetCurrentSplatMap(splatMapIndex);
        if (paintCtrl != null && m_currentSelectIndex >= 0)
        {
            if (paintCtrl.IsTextureArrayNeedUpdate(splatMapIndex))
            {
                Debug.Log("need update");
                paintCtrl.PrepareRuntimeRT();
            }

            //apply brush result to runtime RT start
            var src = RenderTexture.GetTemporary(512, 512, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture.active = src;
            Graphics.Blit(paintCtrl.RunTimePaintTexture[paintCtrl.PaintTextureIndex], src);
            RenderTexture.active = null;
            m_mat.SetTexture("_OldTex", src);
            var dst = RenderTexture.GetTemporary(512, 512, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture.active = dst;
            Graphics.Blit(src, dst, m_mat, 0);
            RenderTexture.active = null;
            RenderTexture.active = paintCtrl.RunTimePaintTexture[paintCtrl.PaintTextureIndex];
            Graphics.Blit(dst, paintCtrl.RunTimePaintTexture[paintCtrl.PaintTextureIndex]);
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(src);
            RenderTexture.ReleaseTemporary(dst);
            //apply brush result to runtime RT end

            //Write runtime RT to source texture
            paintCtrl.WriteRtToTexture2D(paintCtrl.RunTimePaintTexture[paintCtrl.PaintTextureIndex], paintCtrl.SourceTextures[paintCtrl.PaintTextureIndex]);

            var serializeObj = new SerializedObject(paintCtrl);
            var property = serializeObj.FindProperty("_sourceTexture");

            serializeObj.ApplyModifiedProperties();
            //  paintCtrl.SaveToAsset();
        }
        return true;
    }
}
