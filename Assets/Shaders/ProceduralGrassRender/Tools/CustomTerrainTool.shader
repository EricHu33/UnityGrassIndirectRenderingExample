Shader "TerrainTool/CustomTerrainTool"
{
    Properties
    {
        _MainTex ("Texture", any) = "" { }
    }

    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE

        #include "UnityCG.cginc"
        #include "TerrainTool.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

        sampler2D _OldTex;
        float4 _OldTex_ST;

        sampler2D _BrushTex;

        float4 _BrushParams;
        float4 _SplatChannels;
        float _TerrainSize;
        float2 _CursorPosInTerrainUV;
        #define BRUSH_STRENGTH (_BrushParams[0])
        #define BRUSH_TARGETHEIGHT (_BrushParams[1])
        #define BRUSH_SIZE_WS (_BrushParams[2])
        #define kMaxHeight (32766.0f / 65535.0f)

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 pcUV : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 pcUV : TEXCOORD0;
        };

        v2f vert(appdata_t v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.pcUV = v.pcUV;
            return o;
        }

        ENDHLSL

        Pass
        {
            Name "CustomTerrainTool"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                float4 oldCol = tex2D(_OldTex, i.pcUV).rgba;
                float brushUvScale = BRUSH_SIZE_WS / _TerrainSize;
                float hasBrushed = 1 - step(brushUvScale * 0.5, length(_CursorPosInTerrainUV - i.pcUV));
                return saturate((hasBrushed * BRUSH_STRENGTH * _SplatChannels) + oldCol * 1);
            }

            ENDHLSL
        }
    }
}