Shader "MyShader/IndirectDrawObject"
{
    Properties
    {
        _GroundTintRange ("Ground Tint Range", Range(0, 0.8)) = 0.8
        _GroundTintStrength ("Ground Tint Strength", Range(0, 1)) = 0
        [Header(Base Diffuse)]
        _MainTex ("Base Map", 2D) = "white" { }
        _BaseColor ("Base Color", Color) = (1, 1, 1)
        _TopColor ("Top Color", Color) = (1, 1, 1)
        _BaseVariantColor ("Variant Base Color", Color) = (1, 1, 1)
        _TopVariantColor ("Varaint Top Color", Color) = (1, 1, 1)
        _VariantScale ("Color Variant Scale", Range(0, 1)) = 0.01
        _FakeAOMinMax ("FakeAO MinMax", vector) = (0, 1, 0, 0)
        [Header(Vertex Motion)]
        _MotionInfluenceMinMax ("Motion Influence MinMax", Vector) = (0, 1, 0, 0)
        [Header(Object Space)]
        _CameraDistanceScaleFactor ("Camera Distance Scale Factor", Range(0, 1)) = 1
        _ObjectSpaceYScaling ("Object Space Y Scaling", Range(0.5, 6)) = 1
        _ObjectSpaceXZscaling ("Object Space XZ Scaling", Range(0.5, 6)) = 1
        _DistanceScaleUpFactor("Distance Scale Up Factor", Range(0,0.1)) = 0.0225
        [Header(Wind Motion)]
        _Wind1Frequency ("Wind1 Frequency", Float) = 6
        _Wind1Scale ("Wind Scale", Float) = 0.15
        _Wind1Strength ("Wind1 Str", Float) = 0.25
        _SwayAmplitude ("Sway Amplitude", Float) = 0.1
        [Header(Sway Motion)]
        [Header(Specular Lighting)]
        [HDR]_SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularShiness ("Specular Shiness", float) = 18
        _SpecularStrength ("Specular Strength", float) = 1
        _SpecAOMinMax ("Spec AO MinMax", vector) = (0, 1, 0, 0)
        _NormalFadeMinMax ("Normal Fade MinMax", vector) = (55, 75, 0, 0)
        [Header(Translucent)]
        _TranslucentColor ("Translucent Color", Color) = (1, 1, 1, 1)
        _TranslucentAOMinMax ("Translucent AO MinMax", vector) = (0, 1, 0, 0)
        _TranslucentStrength ("Translucent Strength", float) = 0.5
        _TranslucentSharpness ("Translucent Sharpness", float) = 4
        [Header(Debug)]
        _DebugColor ("Debug Color", Color) = (1, 1, 1)

        [Header(Interaction)]
        _PushStrength ("Push Strength", Float) = 1
        _PushDownStrength ("Push Down Strength", Float) = 0.5
        _WigglePowerAfterPush ("Wiggle After Push", Float) = 1
        _WiggleFrequency ("Wiggle Frequency", Float) = 4
        [HDR]_FireColor ("Fire Color", Color) = (1, 0, 0, 1)
        [HDR]_FreezeColor ("Freeze Color", Color) = (0, 0.8, 0, 1)
        [Header(Random Range)]
        _RadomWidthRange ("Radom Width Range", Float) = 1
        _RadomHeightRange ("Random Height Range", Float) = 1
        //[Header(Distance Fade)]
        //_MaxDistance ("Max Distance", Float) = 90
        //_FadeInDistance ("Fade In Distance", Float) = 60

        [Toggle(_BILLBOARD)] _BillboardEnabled ("Enable Billboard", Float) = 0.0
    }
    SubShader
    {
        Cull Off
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "SimpleLit" "IgnoreProjector" = "True" "ShaderModel" = "4.5" }
        LOD 300

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma shader_feature_local _BILLBOARD

            // -------------------------------------
            #include "Common/Quaternion.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShaderInclude_ProceduralInstancingGrass.hlsl"
            Texture2D _GroundTintMap;
            SamplerState sampler_GroundTintMap;

            float _BendingFromSurfaceNoraml;
            float _BendingFromTopStr;
            
            float _GroundTintStrength;
            float _GroundTintRange;

            uint _IsGammaColorSpace;
            //Base Diffuse
            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
            float4 _BaseVariantColor;
            float4 _TopColor;
            float4 _TopVariantColor;
            float _VariantScale;
            float3 _FakeAOMinMax;
            //

            //Bezier Motion
            float _BezierInfluencePower;
            float2 _MotionInfluenceMinMax;
            //
            
            //Object Space
            float _HasFlowerTop;
            float _CameraDistanceScaleFactor;
            float _ObjectSpaceYScaling;
            float _ObjectSpaceXZscaling;
            float _DistanceScaleUpFactor;
            //

            //Wind Motion
            float _Wind1Frequency;
            float _Wind1Scale;
            float _Wind1Strength;
            float _SwayAmplitude;

            //Specular Light
            float4 _SpecularColor;
            float _SpecularShiness;
            float _SpecularStrength;
            float2 _SpecAOMinMax;
            float2 _NormalFadeMinMax;
            //

            //Translucent
            float4 _TranslucentColor;
            float2 _TranslucentAOMinMax;
            float _TranslucentStrength;
            float _TranslucentSharpness;

            float4 _DebugColor;
            
            //Interactive
            float _PushStrength;
            float _PushDownStrength;
            float _WigglePowerAfterPush;
            float _WiggleFrequency;
            float4 _FireColor;
            float4 _FreezeColor;
            //

            //Random Offset
            float _RadomWidthRange;
            float _RadomHeightRange;
            //

            //LOD fading
            float _MaxDistance;
            float _FadeInDistance;

            float _CloseDistance;
            float _MidDistance;
            float _FarDistance;

            float3 _InteractMotionCenterWS;
            float _InteractMapSize;
            float _TerrainSize;


            //float _DitherThreshold;
            //float _DitherTexelSize;
            //float2 _CrossFadeMinMax;

            Texture2D _InteractMap;
            SamplerState sampler_InteractMap;
            
            
            inline float GammaToLinearSpaceExact(float value)
            {
                if (value <= 0.04045F)
                    return value / 12.92F;
                else if (value < 1.0F)
                    return pow((value + 0.055F) / 1.055F, 2.4F);
                else
                    return pow(value, 2.2F);
            }

            inline half3 GammaToLinearSpace(half3 sRGB)
            {
                // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);

                // Precise version, useful for debugging.
                //return half3(GammaToLinearSpaceExact(sRGB.r), GammaToLinearSpaceExact(sRGB.g), GammaToLinearSpaceExact(sRGB.b));

            }

            inline float LinearToGammaSpaceEXExact(float value)
            {
                if (value <= 0.0F)
                    return 0.0F;
                else if (value <= 0.0031308F)
                    return 12.92F * value;
                else if (value < 1.0F)
                    return 1.055F * pow(value, 0.4166667F) - 0.055F;
                else
                    return pow(value, 0.45454545F);
            }

            inline half3 LinearToGammaSpaceEX(half3 linRGB)
            {
                linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
                // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);

                // Exact version, useful for debugging.
                //return half3(LinearToGammaSpaceEXExact(linRGB.r), LinearToGammaSpaceEXExact(linRGB.g), LinearToGammaSpaceEXExact(linRGB.b));

            }


            inline float noise_randomValue(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            inline float noise_interpolate(float a, float b, float t)
            {
                return (1.0 - t) * a + (t * b);
            }

            inline float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                uv = abs(frac(uv) - 0.5);
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0 = noise_randomValue(c0);
                float r1 = noise_randomValue(c1);
                float r2 = noise_randomValue(c2);
                float r3 = noise_randomValue(c3);

                float bottomOfGrid = noise_interpolate(r0, r1, f.x);
                float topOfGrid = noise_interpolate(r2, r3, f.x);
                float t = noise_interpolate(bottomOfGrid, topOfGrid, f.y);
                return t;
            }

            float SimpleNoise(float2 UV, float Scale)
            {
                float t = 0.0;

                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3 - 0));
                t += valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3 - 1));
                t += valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3 - 2));
                t += valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                return t;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                float4 positionSS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD4;
                #endif

                float3 surfaceUp : TEXCOORD5;
                float3 debugColor : TEXCOORD6;
                float3 albedo : TEXCOORD7;
                float3 translucent : TEXCOORD8;
                float3 specular : TEXCOORD9;
                float4 interactData : TEXCOORD10;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 11);
            };

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID, uint vid : SV_VertexID)
            {
                Varyings o;
                float3 instanceRootWS = _MeshDatas[instanceID].Position;
                float3 surfaceUp = _MeshDatas[instanceID].Up;
                float noise1 = 0, noise2 = 0, colorVariantNoise = 0;
                float hasFlowerTop = step(0.1, _HasFlowerTop);

                //interactive grass - start
                //terrain size = 512
                float2 pushAwayUv = float2(floor(instanceRootWS.xz - _InteractMotionCenterWS.xz) / _TerrainSize);
                //   float3 wpos = float3(id.x / 256.0, 0, id.y / 256.0) * _TerrainSize + _InteractMotionCenterWS;
                //interact direction
                //r = dirX,
                //g = dirZ,
                //b = trailRemain,
                //a = type
                
                float4 interactData = SAMPLE_TEXTURE2D_LOD(_InteractMap, sampler_InteractMap, pushAwayUv, 0);
                o.interactData = interactData;
                //wind noise
                GradientNoise(instanceRootWS.xz + _Time.y * _Wind1Frequency + IN.uv.y * 1, _Wind1Scale, noise1);
                noise1 = (noise1 - 0.5) ;
                noise1 *= _Wind1Strength;

                noise2 = noise1;
                //wind noise
                
                float3 positionWS = 0;
                float3 normalWS = 0;

                //object scaling
                float3 viewWS = _WorldSpaceCameraPos - instanceRootWS;
                float viewWSLength = length(viewWS);
                float3 posOS = IN.positionOS;
                
                posOS.xz *= _ObjectSpaceXZscaling;
                posOS.y *= _ObjectSpaceYScaling;
                //TODO refactor to somewhere else.....
                //////////////////////
                //Interact Behaviour//
                //////////////////////


                DistanceScaleFade_float(instanceID, _MaxDistance, _FadeInDistance, instanceRootWS, posOS, posOS);

                ProceduralBezierMotion_float(
                    instanceID, vid,
                    instanceRootWS, posOS, IN.normalOS, surfaceUp,
                    noise1, noise2, IN.uv, IN.uv2,
                    0, 0, _MotionInfluenceMinMax,
                    0, interactData, _SwayAmplitude,
                    _PushStrength, _PushDownStrength, _RadomWidthRange, _RadomHeightRange, _WigglePowerAfterPush, _WiggleFrequency,_DistanceScaleUpFactor,
                    positionWS, normalWS);

                o.pos = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0f));
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                o.fogFactor = ComputeFogFactor(o.pos.z);
                o.positionSS = ComputeScreenPos(o.pos);
                o.positionWS = positionWS;
                o.surfaceUp = surfaceUp;
                o.debugColor = 0;

                float aoAlongUV = IN.uv2.y;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    o.shadowCoord = TransformWorldToShadowCoord(positionWS);
                #endif
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                float nDotL = saturate(dot(normalWS, mainLight.direction));
                float fixedNdotL = saturate(dot(surfaceUp, mainLight.direction));
                float3 fakeSSR = (mainLight.direction + surfaceUp * float3(0.2, 0.2, 0.2)) * - 1;
                float3 GI = SampleSH(surfaceUp);
                float fixedShadingGI = (fixedNdotL * 0.5 + 0.5);
                o.debugColor = IN.uv.yyy;
                o.debugColor = lerp(float3(0, 1, 0), lerp(float3(1, 1, 0), float3(1, 0, 1), step(_MidDistance + 1, _MeshDatas[instanceID].MaxDrawDistance)), step(_CloseDistance + 1, _MeshDatas[instanceID].MaxDrawDistance));
                //color variant noise
                GradientNoise(instanceRootWS.xz, _VariantScale, colorVariantNoise);
                //color variant noise
                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - positionWS.xyz);
                //Base Color
                float3 albedoBottom = lerp(_BaseColor, _BaseVariantColor, colorVariantNoise) * fixedShadingGI;
                //TODO mapping with actual terrain size
                float2 groundTintUV = ((o.positionWS.xz - _InteractMotionCenterWS.xz) % _TerrainSize) / _TerrainSize;
                float3 groundTintColor = SAMPLE_TEXTURE2D_LOD(_GroundTintMap, sampler_GroundTintMap, groundTintUV, 0);
                
                //groundTintColor = (1 - _IsGammaColorSpace) * LinearToGammaSpaceEX(groundTintColor) + (_IsGammaColorSpace) * groundTintColor;
                //groundTintColor = GammaToLinearSpace(groundTintColor);
                groundTintColor = groundTintColor * fixedNdotL * mainLight.distanceAttenuation;
                float groundTintRange = 1 - smoothstep(_GroundTintRange, 0.8, aoAlongUV);
                albedoBottom = lerp(albedoBottom, groundTintColor, groundTintRange * _GroundTintStrength);
                
                float3 albedTop = lerp(_TopColor, _TopVariantColor, colorVariantNoise) * fixedShadingGI;
                float3 baseColor = lerp(albedoBottom, albedTop, smoothstep(_FakeAOMinMax.x, _FakeAOMinMax.y, aoAlongUV));
                o.albedo = baseColor;

                //TODO option to choose between NdotL and fixedNdotL
                //Translucency Color
                float translucent01 = smoothstep(_TranslucentAOMinMax.x, _TranslucentAOMinMax.y, aoAlongUV);
                float subSurfaceScattering = pow(saturate(dot(fakeSSR, viewDirectionWS)), _TranslucentSharpness) * _TranslucentStrength * length(mainLight.color);
                o.translucent = subSurfaceScattering * translucent01 * _TranslucentColor.rgb;

                //Specular Color
                float3 halfVec = normalize(mainLight.direction + viewDirectionWS);
                float nDotH = saturate(dot(normalWS, halfVec));
                float spec = pow(nDotH, _SpecularShiness) * _SpecularStrength;
                float specAO = smoothstep(_SpecAOMinMax.x, _SpecAOMinMax.y, aoAlongUV);
                o.specular = _SpecularColor.rgb * spec * specAO;

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(surfaceUp, o.vertexSH);

                return o;
            }

            //0.4ms

            float4 frag(Varyings i) : SV_Target
            {
                // return float4(i.debugColor, 1);
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(baseColor.a - 0.5);
                #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
                    half4 shadowMask = inputData.shadowMask;
                #elif !defined(LIGHTMAP_ON)
                    half4 shadowMask = unity_ProbesOcclusion;
                #else
                    half4 shadowMask = half4(1, 1, 1, 1);
                #endif

                float4 shadowCoord = float4(0, 0, 0, 0);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    shadowCoord = i.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #else
                    shadowCoord = float4(0, 0, 0, 0);
                #endif

                #if VERSION_GREATER_EQUAL(10, 0)
                    Light mainLight = GetMainLight(shadowCoord, i.positionWS, shadowMask);
                #else
                    Light mainLight = GetMainLight(shadowCoord);
                #endif
                /* float3 bakedGI = SampleSH(i.surfaceUp);
                bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, i.surfaceUp);
                MixRealtimeAndBakedGI(mainLight, i.surfaceUp, bakedGI);*/
                float lightAttenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                float3 lightDir = mainLight.direction;

                float3 finalColor = i.albedo;
                finalColor += i.specular;
                finalColor += i.translucent;
                finalColor = finalColor * lightAttenuation * mainLight.color;
                //return float4(i.albedo * lightAttenuation * mainLight.color, 1);
                
                //color by interaction
                
                finalColor = MixFog(finalColor, i.fogFactor);
                //Fire Color
                float2 fireUV1 = i.positionWS.xz + _Time.yy * 0.1;
                float2 fireUV2 = i.positionWS.xz - _Time.yy * 0.1;
                float fireNoise1 = SimpleNoise(fireUV1, 50) * 2;
                float fireNoise2 = SimpleNoise(fireUV2, 50) * 2;
                float fireNoiseCombine = step(fireNoise1 * fireNoise2, 0.5);
                float3 fireColor = _FireColor.rgb * fireNoiseCombine;
                float3 freezeColor = lerp(_FreezeColor.rgb, finalColor, 0.5);
                float fireProgress = i.interactData.b * IncludeFromType(floor(i.interactData.a), 4);
                float freezeProgress = i.interactData.b * IncludeFromType(floor(i.interactData.a), 5);
                finalColor = lerp(finalColor, fireColor, smoothstep(0.3, 0.8, fireProgress));
                finalColor = lerp(finalColor, freezeColor, smoothstep(0.3, 0.8, freezeProgress));
                
                return float4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}