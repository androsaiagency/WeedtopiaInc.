using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Canvas geometry in density-independent display units; Android Screen.dpi reports densityDpi.</summary>
    public static class UiDisplayMetrics
    {
#if UNITY_EDITOR
        // Used only by the explicitly started Game View evidence harness, never serialized into a scene or build.
        public static float EvidenceDensity;
#endif
        public static float Density
        {
            get
            {
#if UNITY_EDITOR
                if(EvidenceDensity>0)return EvidenceDensity;
#endif
                return UnityEngine.Application.isMobilePlatform&&Screen.dpi>0?Mathf.Clamp(Screen.dpi/160f,1,4):1;
            }
        }
        public static float Scale(Canvas canvas)=>Mathf.Max(.01f,canvas.scaleFactor/Density);
    }
}
