using Unity.Collections;
using UnityEngine.Experimental.U2D;

namespace Unity.U2D.Entities
{
    internal static class CommonHybridUtils
    {
        public static void ClearSpriteRendererGroup()
        {
            SpriteRendererGroup.Clear();
        }

        public static void AddToSpriteRendererGroup(NativeArray<HybridRendererInfo> renderers)
        {
            var rendererInfos = renderers.Reinterpret<SpriteIntermediateRendererInfo>();
            SpriteRendererGroup.AddRenderers(rendererInfos);
        }
    }
}