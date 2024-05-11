
void MyGetLightDirection_float(out float3 Out)
{
    #if SHADERGRAPH_PREVIEW
        Out = 1;
    #else
        Out = 0;
        Light mainLight = GetMainLight();
        Out = mainLight.direction;
    #endif
}


void MyGetLightDirection_half(out float3 Out)
{
    #if SHADERGRAPH_PREVIEW
        Out = 1;
    #else
        Out = 0;
        Light mainLight = GetMainLight();
        Out = mainLight.direction;
    #endif
}


void MyGetLightColor_float(out float3 Out)
{
    #if SHADERGRAPH_PREVIEW
        Out = 1;
    #else
        Out = 0;
        Light mainLight = GetMainLight();
        Out = mainLight.color;
    #endif
}


void MyGetLightColor_half(out float3 Out)
{
    #if SHADERGRAPH_PREVIEW
        Out = 1;
    #else
        Out = 0;
        Light mainLight = GetMainLight();
        Out = mainLight.color;
    #endif
}

void MyGetLightDistanceAtten_float(out float Out)
{
    #if SHADERGRAPH_PREVIEW
        Out = 1;
    #else
        Out = 0;
        Light mainLight = GetMainLight();
        Out = mainLight.distanceAttenuation;
    #endif
}

void MyGetLightDistanceAtten_half(out float Out)
{
    #if SHADERGRAPH_PREVIEW
        Out = 1;
    #else
        Out = 0;
        Light mainLight = GetMainLight();
        Out = mainLight.distanceAttenuation;
    #endif
}
