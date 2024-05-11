   using UnityEngine;

   public class MagicGrassProperty
    {
        public static int _SplatChannels = Shader.PropertyToID("_SplatChannels");
        public static int _TerrainPivotWS = Shader.PropertyToID("_TerrainPivotWS");
        public static int _ExcludeThreshold = Shader.PropertyToID("_ExcludeThreshold");
        public static int _IncludeThreshold = Shader.PropertyToID("_IncludeThreshold");
        public static int _TerrainSize = Shader.PropertyToID("_TerrainSize");
        public static int _MapSize = Shader.PropertyToID("_MapSize");
        public static int _GrassDensity = Shader.PropertyToID("_GrassDensity");
        public static int _DrawGrassResult = Shader.PropertyToID("_DrawGrassResult");
        public static int _CameraPositionWS = Shader.PropertyToID("_CameraPositionWS");
        public static int _TwiceTerrainHeight = Shader.PropertyToID("_TwiceTerrainHeight");
        public static int _HeightMap = Shader.PropertyToID("_HeightMap");
        public static int _MatrixVP = Shader.PropertyToID("_MagicGrassCameraMatrixVP");
        public static int _EnabledCull = Shader.PropertyToID("_EnabledCull");
        public static int _MaxInstanceCount = Shader.PropertyToID("_MaxInstanceCount");
        public static int _HiZTextureSize = Shader.PropertyToID("_HiZTextureSize");
        public static int _HiZMap = Shader.PropertyToID("_HiZMap");
        public static int _CullInputs = Shader.PropertyToID("_CullInputs");
        public static int _InputCounterBuffer = Shader.PropertyToID("_InputCounterBuffer");
        public static int _Lod0CullResult = Shader.PropertyToID("_Lod0CullResult");
        public static int _MyMainLightDirection = Shader.PropertyToID("_MyMainLightDirection");
        public static int _InteractMotionCenterWS = Shader.PropertyToID("_InteractMotionCenterWS");
        public static int _InteractPrograssMap = Shader.PropertyToID("_InteractProgressMap");
        public static int _InteractMap = Shader.PropertyToID("_InteractMap");
        public static int _GroundTintMap = Shader.PropertyToID("_GroundTintMap");
        public static int _InteractMapSize = Shader.PropertyToID("_InteractMapSize");
        public static int _CloseDistance = Shader.PropertyToID("_CloseDistance");
        public static int _MidDistance = Shader.PropertyToID("_MidDistance");
        public static int _FarDistance = Shader.PropertyToID("_FarDistance");
        public static int _MaxDinstance = Shader.PropertyToID("_MaxDistance");
        public static int _FadeInDistance = Shader.PropertyToID("_FadeInDistance");
    }

    public enum MagicGrassGrassMapSize
    {
        Low = 128,
        Mid = 256,
        High = 512,
    }