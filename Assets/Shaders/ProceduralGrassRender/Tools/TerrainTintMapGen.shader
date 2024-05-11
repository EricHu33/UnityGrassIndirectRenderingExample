Shader "Unlit/TerrainTintMapGen"
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

            sampler2D _Diffuse0;
            sampler2D _Diffuse1;
            sampler2D _Diffuse2;
            sampler2D _Diffuse3;
            sampler2D _DetailMap;
            

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

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
                //100 = terrain height
                //200 = terrain width & length
                float hL = UnpackHeightmap(tex2D(_MainTex, i.uv.xy - offset.xz * _MainTex_TexelSize.x * 2)) * 100 / 200;
                float hR = UnpackHeightmap(tex2D(_MainTex, i.uv.xy + offset.xz * _MainTex_TexelSize.x * 2)) * 100 / 200;
                float hD = UnpackHeightmap(tex2D(_MainTex, i.uv.xy - offset.zy * _MainTex_TexelSize.y * 2)) * 100 / 200;
                float hU = UnpackHeightmap(tex2D(_MainTex, i.uv.xy + offset.zy * _MainTex_TexelSize.y * 2)) * 100 / 200;

                // float3 N = normalize(cross(normalize(float3(0, hU - hD, 2)), normalize(float3(2, hR - hL, 0))));
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
