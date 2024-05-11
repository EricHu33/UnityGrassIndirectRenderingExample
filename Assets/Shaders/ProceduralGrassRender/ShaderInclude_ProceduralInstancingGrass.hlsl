
#ifndef PROCEDURL_INSTANCING_GRASS_INCLUDED
#define PROCEDURL_INSTANCING_GRASS_INCLUDED

struct MeshData
{
    float3 Position;
    float3 Forward;
    float3 Up;
    float MaxDrawDistance;
    float BandsFading;
    float Pad1;
};

#include "Common/Quaternion.hlsl"
#if defined(SHADEROPTIONS_CAMERA_RELATIVE_RENDERING) && SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0
    //  #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#endif

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
StructuredBuffer<MeshData> _MeshDatas;

float hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * .1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 gradientNoise_dir(float2 p)
{
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float gradientNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(gradientNoise_dir(ip), fp);
    float d01 = dot(gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
}

void GradientNoise(float2 UV, float Scale, out float Out)
{
    Out = gradientNoise(UV * Scale) + 0.5;
}

void UnityDither(float In, float4 ScreenPosition)
{
    float DitherTexelSize = 8;
    float2 uv = ScreenPosition.xy * _ScreenParams.xy / DitherTexelSize;
    float DITHER_THRESHOLDS[16] = {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    clip(In - DITHER_THRESHOLDS[index]);
}

void vertInstancingMatrices(out float4x4 objectToWorld, out float4x4 worldToObject)
{
    #if UNITY_ANY_INSTANCING_ENABLED
        MeshData data = _MeshDatas[unity_InstanceID];

        // transform matrix
        objectToWorld._11_21_31_41 = float4(1, 0, 0, 0);
        objectToWorld._12_22_32_42 = float4(0, 1, 0, 0);
        objectToWorld._13_23_33_43 = float4(0, 0, 1, 0);
        objectToWorld._14_24_34_44 = float4(data.Position, 1.0f);

        // inverse transform matrix
        float3x3 w2oRotation;
        w2oRotation[0] = objectToWorld[1].yzx * objectToWorld[2].zxy - objectToWorld[1].zxy * objectToWorld[2].yzx;
        w2oRotation[1] = objectToWorld[0].zxy * objectToWorld[2].yzx - objectToWorld[0].yzx * objectToWorld[2].zxy;
        w2oRotation[2] = objectToWorld[0].yzx * objectToWorld[1].zxy - objectToWorld[0].zxy * objectToWorld[1].yzx;

        float det = dot(objectToWorld[0].xyz, w2oRotation[0]);

        w2oRotation = transpose(w2oRotation);

        w2oRotation *= rcp(det);

        float3 w2oPosition = mul(w2oRotation, -objectToWorld._14_24_34);

        worldToObject._11_21_31_41 = float4(w2oRotation._11_21_31, 0.0f);
        worldToObject._12_22_32_42 = float4(w2oRotation._12_22_32, 0.0f);
        worldToObject._13_23_33_43 = float4(w2oRotation._13_23_33, 0.0f);
        worldToObject._14_24_34_44 = float4(w2oPosition, 1.0f);
    #endif
}


// use #pragma instancing_options procedural:vertInstancingSetup to setup unity_InstanceID & related macro
#if UNITY_ANY_INSTANCING_ENABLED
    void vertInstancingSetup()
    {
        // vertInstancingMatrices(_grassObject2world, _grassWorld2object);

    }
#endif

void Get_M_IM_Matrix_float(out float4x4 object2world, out float4x4 world2object)
{
    object2world = (
        1.f, 0.f, 0.f, 0.f,
        0.f, 1.f, 0.f, 0.f,
        0.f, 0.f, 1.f, 0.f,
        0.f, 0.f, 0.f, 0.f
    );

    world2object = (
        1.f, 0.f, 0.f, 0.f,
        0.f, 1.f, 0.f, 0.f,
        0.f, 0.f, 1.f, 0.f,
        0.f, 0.f, 0.f, 0.f
    );
    #if UNITY_ANY_INSTANCING_ENABLED
        vertInstancingMatrices(object2world, world2object);
        #if defined(SHADEROPTIONS_CAMERA_RELATIVE_RENDERING) && SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0
            object2world = ApplyCameraTranslationToMatrix(object2world);
            world2object = ApplyCameraTranslationToInverseMatrix(world2object);
        #endif
    #endif
}

float3 GetCurvePoint(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    t = saturate(t);
    float oneMinusT = 1 - t;
    float3 result = oneMinusT * oneMinusT * oneMinusT * p0
    + 3 * (oneMinusT * oneMinusT) * t * p1
    + 3 * oneMinusT * t * t * p2
    + t * t * t * p3;
    return result;
}

float3 GetNormalAlongCurve(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    t = saturate(t);
    float oneMinusT = 1 - t;
    float3 normal = 3 * oneMinusT * oneMinusT * (p1 - p0)
    + 6 * oneMinusT * t * (p2 - p1)
    + 3 * t * t * (p3 - p2);
    return normal;
}

// Shader Graph Functions
void GetInstancingRootPosWS_float(out float3 Out)
{
    Out = 0;
    #if UNITY_ANY_INSTANCING_ENABLED
        Out = _MeshDatas[unity_InstanceID].Position;
    #endif
}

void GetInstancingPos_float(in float3 PositionWS, out float3 Out)
{
    Out = 0;
    #if UNITY_ANY_INSTANCING_ENABLED
        Out = PositionWS + _MeshDatas[unity_InstanceID].Position;
    #endif
}


void GetInstancingSurfaceUpDir_float(out float3 Out)
{
    Out = 0;
    #if UNITY_ANY_INSTANCING_ENABLED
        Out = _MeshDatas[unity_InstanceID].Up;
    #endif
}

void RotateToInstancingForward_float(in float3 PositionWS, out float3 Out)
{
    Out = 0;
    #if UNITY_ANY_INSTANCING_ENABLED
        float3 forwardWS = _MeshDatas[unity_InstanceID].Forward;
        float4 rot = from_to_rotation(float3(0, 0, 1), forwardWS);
        Out = rotate_vector(PositionWS, rot);
    #endif
}

void GetInstancingRootForwardWS_float(out float3 Out)
{
    Out = 0;
    #if UNITY_ANY_INSTANCING_ENABLED
        Out = _MeshDatas[unity_InstanceID].Forward;
    #endif
}

void RotateForBillboard_float(in float3 PositionWS, out float3 Out)
{
    Out = float3(0, 0, 0);
    Out = PositionWS.x * UNITY_MATRIX_V[0].xyz;
    Out += PositionWS.y * UNITY_MATRIX_V[1].xyz;
}

//_WorldSpaceCameraPos may need to change in HDRP for relative rendering
void DistanceScaleFade_float(uint InstanceID, float MaxDinstance, float FadeInDistance, float3 RootPositionWS, float3 PositionOS, out float3 Out)
{
    Out = 0;
    float maxDistance = lerp(MaxDinstance, _MeshDatas[InstanceID].MaxDrawDistance, step(0.1, _MeshDatas[InstanceID].BandsFading));
    float fadingRange = FadeInDistance;
    float startFadingDistance = maxDistance - fadingRange;
    float normalizedDistance = (distance(RootPositionWS, _WorldSpaceCameraPos) - startFadingDistance) / (maxDistance - startFadingDistance);
    float distanceFactor = saturate(normalizedDistance);
    PositionOS *= (1 - distanceFactor);
    Out = PositionOS;
}

//_WorldSpaceCameraPos may need to change in HDRP for relative rendering
void DistanceAntiAliasingScale_float(uint InstanceID, float MaxDinstance, float FadeInDistance, float3 RootPositionWS, float3 PositionOS, out float3 Out)
{
    Out = 0;
    float maxDistance = lerp(MaxDinstance, _MeshDatas[InstanceID].MaxDrawDistance, step(0.1, _MeshDatas[InstanceID].BandsFading));
    float fadingRange = FadeInDistance;
    float startFadingDistance = maxDistance - fadingRange;
    float normalizedDistance = (distance(RootPositionWS, _WorldSpaceCameraPos) - startFadingDistance) / (maxDistance - startFadingDistance);
    float distanceFactor = saturate(normalizedDistance);
    PositionOS *= (1 - distanceFactor);
    Out = PositionOS;
}

void LinearToGammaSpaceEX_float(float3 linRGB, out float3 OUT)
{
    OUT = float3(0, 0, 0);
    linRGB = max(linRGB, float3(0, 0, 0));
    // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    OUT = max(1.055 * pow(linRGB, 0.416666667) - 0.055, 0);
    // Exact version, useful for debugging.
    //return half3(LinearToGammaSpaceEXExact(linRGB.r), LinearToGammaSpaceEXExact(linRGB.g), LinearToGammaSpaceEXExact(linRGB.b));

}

float IncludeFromType(uint currentSampleType, uint targetType)
{
    return currentSampleType == targetType ? 1 : 0;
}

float ExcludeFromType(float currentSampleType, float targetType)
{
    return currentSampleType == targetType ? 0 : 1;
}


void IncludeFromType_float(uint currentSampleType, uint targetType, out float Out)
{
    Out = 0;
    Out = currentSampleType == targetType ? 1 : 0;
}

float EaseOutBack(float x)
{
    return pow(x, 4.0);
}

void ProceduralBezierMotion_float(
    in uint instanceID,
    in uint vid,
    in float3 instanceRootWS,
    in float3 positionOS,
    in float3 normalOS,
    in float3 surfaceUpWS,
    in float noise1,
    in float noise2,
    in float2 uv,
    in float2 uv2,
    in float bendingFromTopStr,
    in float bendingFromSurfaceNoraml,
    in float2 motionInfluenceMinMax,
    in float hasFlowerTop,
    in float4 interactData,
    in float swayAmplitude,
    in float pushPower,
    in float pushDownPower,
    in float radomWidthRange,
    in float radomHeightRange,
    in float wigglePowerAfterPush,
    in float wiggleFreq,
    in float distanceScaleUpFactor,
    out float3 OutPositionWS,
    out float3 OutNormalWS)
{
    OutPositionWS = 0;
    OutNormalWS = 0;
    
    //wind noise
    float2 offsetWS = instanceRootWS.xz;
    float curveFallOff = uv2.y;
    curveFallOff = smoothstep(motionInfluenceMinMax.x, motionInfluenceMinMax.y, curveFallOff);
    //wind noise

    //motion test
    float sinFallOff = uv2.y;
    
    //Interact Behaviour//
    //////////////////////
    //WalkPusher = 1,
    //Explosion = 2,
    //Absorb = 3,
    //Burnning = 4,
    //Freeze = 5,

    //pushAwayDaya
    //x/r = dirX,
    //y/g = dirZ,
    //z/b = trailRemain,
    //w/a = type

    float2 trailPushDir = interactData.rg;
    float trailRemain = interactData.b;
    float trailType = floor(interactData.a);

    float hasWindSway = 1 - trailRemain;
    float interactTypeReversePushFilter = IncludeFromType(trailType, 3);
    float interactTypeWiggleFilter = trailRemain * ExcludeFromType(trailType, 4) * ExcludeFromType(trailType, 5);
    float interactTypePushDownFilter = ExcludeFromType(trailType, 5);
    float interactTypePushAwayFilter = ExcludeFromType(trailType, 4) * ExcludeFromType(trailType, 5);

    float3 swayZ = hasWindSway.rrr * float3(noise1, 0, noise2);
    float wiggleNoise = sin(offsetWS.xy * 3 + _Time.y * wiggleFreq);
    float2 wiggleSway = interactTypeWiggleFilter * saturate(abs(trailPushDir)) * sinFallOff;
    
    float pushDownAmount = interactTypePushDownFilter * - pushDownPower * trailRemain;
    float3 pushDirAll = interactTypePushAwayFilter * float3(trailPushDir.x, 0, trailPushDir.y) * pushPower * trailRemain;
    pushDirAll = -1 * pushDirAll * interactTypeReversePushFilter + (1 - interactTypeReversePushFilter) * pushDirAll;
    pushDirAll.y = pushDownAmount;

    float3 forwardWS = _MeshDatas[instanceID].Forward;
    float4 facingRot = from_to_rotation(float3(0, 0, 1), forwardWS);
    float4 rotUp = from_to_rotation(float3(0, 1, 0), surfaceUpWS);
    float4 allRot = qmul(rotUp, facingRot);
    #if defined(_BILLBOARD)
        allRot = rotUp;
    #endif

    float3 adjustSurfaceUp = normalize(surfaceUpWS - swayZ);
    float4 windRot = from_to_rotation(surfaceUpWS, adjustSurfaceUp);
    OutNormalWS = rotate_vector(surfaceUpWS, windRot);
    //normal

    //object to world
    float3 posOS = positionOS.xyz;
    float3 localToWS = posOS;
    localToWS.xz *= (1 + ((hash12(offsetWS) - 0.5) * radomWidthRange));
    localToWS.y *= (1 + ((hash12(offsetWS) - 0.5) * radomHeightRange));
    localToWS = rotate_vector(localToWS, allRot);

    // TODO aniti aliasing stretch. should adjust by bands?
    float3 viewWS = _WorldSpaceCameraPos - instanceRootWS;
    float viewWSLength = length(viewWS);
    localToWS.x += localToWS.x * max(0, viewWSLength * 0.003);

    localToWS.xz += swayZ * curveFallOff;
    // localToWS.y += swayZ * curveFallOff;
    //extra push
    //float swayUpDown = -0.5 * (sin(offsetWS.x * 5 + offsetWS.y * 5 + uv2.y + _Time.y * 5.0) + 1.0) * swayAmplitude * curveFallOff * 0.5;
   // localToWS.y += swayUpDown;
    
    OutPositionWS = instanceRootWS;
    #if defined(_BILLBOARD)
        
        float3 camPosOS = _WorldSpaceCameraPos;
        float3 bNormalDir = camPosOS - instanceRootWS;
        bNormalDir.y = 0;
        bNormalDir = normalize(bNormalDir);
        // TODO custom billboard fade range
        // float billbloardFade = (distance(instanceRootWS, _WorldSpaceCameraPos) - 20) / (30 - 20);
        // bNormalDir = lerp(float3(0, 0, -1), bNormalDir, saturate(billbloardFade));
        
        float3 objectRight = mul((float3x3)unity_WorldToObject, cross(OutNormalWS, forwardWS));
        float3 objectUp = mul((float3x3)unity_WorldToObject, OutNormalWS);
        float3 bUpDir = abs(bNormalDir.y) > 0.999 ? normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1))) : normalize(surfaceUpWS);
        //    float3 bUpDir = abs(bNormalDir.y) > 0.999 ? cross(surfaceUpWS, forwardWS) : surfaceUpWS;
        float3 rightDir = normalize(cross(bUpDir, bNormalDir));
        bUpDir = normalize(cross(bNormalDir, rightDir));
        float3 billboardLocalPos = rightDir * localToWS.x + bUpDir * localToWS.y + bNormalDir * localToWS.z;
        billboardLocalPos += (pushDirAll * sinFallOff);
        billboardLocalPos.xz += wigglePowerAfterPush * (trailPushDir.xy) * wiggleNoise * wiggleSway * saturate(1 - length(pushDirAll));
        localToWS = billboardLocalPos;
    #else
        
        localToWS += (pushDirAll * sinFallOff);
        localToWS.xz += wigglePowerAfterPush * (trailPushDir.xy) * wiggleNoise * wiggleSway * saturate(1 - 2 * length(pushDirAll));
    
    #endif
    float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;
    //float3 viewWS = _WorldSpaceCameraPos - instanceRootWS;
    //float viewWSLength = length(viewWS);
    localToWS += cameraTransformRightWS * dot(localToWS.xz, cameraTransformRightWS) * max(0, viewWSLength * distanceScaleUpFactor);
    OutPositionWS += localToWS;
    
    float subRootY = localToWS.y  - distance(0,   localToWS);
    float3 sphereDir = normalize(localToWS - float3(0,0,0));
    
    OutPositionWS+=  subRootY * sphereDir * 0.5;
}



void GetInstancedID_float(out uint Out)
{
    Out = 0;
    #if UNITY_ANY_INSTANCING_ENABLED
        Out = unity_InstanceID;
    #endif
}

#endif