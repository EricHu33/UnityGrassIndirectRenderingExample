Shader "Unlit/NormalGen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _TerrainHeight;
            float _TerrainWidthLength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 offset = fixed3(1.0, 1.0, 0.0);
                // x/y
                //x = terrain height
                //y = terrain width & length
                float hL = UnpackHeightmap(tex2D(_MainTex, i.uv.xy - offset.xz * _MainTex_TexelSize.x * 2)) * _TerrainHeight / _TerrainWidthLength;
                float hR = UnpackHeightmap(tex2D(_MainTex, i.uv.xy + offset.xz * _MainTex_TexelSize.x * 2)) * _TerrainHeight / _TerrainWidthLength;
                float hD = UnpackHeightmap(tex2D(_MainTex, i.uv.xy - offset.zy * _MainTex_TexelSize.y * 2)) * _TerrainHeight / _TerrainWidthLength;
                float hU = UnpackHeightmap(tex2D(_MainTex, i.uv.xy + offset.zy * _MainTex_TexelSize.y * 2)) * _TerrainHeight / _TerrainWidthLength;

                float3 N = 0;
                N.x = -2.0 * (hR - hL);
                N.y = 2.0f * _MainTex_TexelSize.x * 2;
                N.z = (hU - hD) * - 2.0;
                N = normalize(N);
                return fixed4(N.rgb, 1);
            }
            ENDCG
        }
    }
}
