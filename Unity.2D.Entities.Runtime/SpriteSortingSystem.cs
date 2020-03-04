using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using Camera = Unity.Tiny.Rendering.Camera;

namespace Unity.U2D.Entities
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(SpriteRendererCullingSystem))]
    internal class SpriteSortingSystem : JobComponentSystem
    {
        private struct RenderItemComparer : System.Collections.Generic.IComparer<RenderItem>
        {
            public int Compare(RenderItem lhs, RenderItem rhs)
            {
                if (lhs.LayerAndOrder != rhs.LayerAndOrder)
                    return lhs.LayerAndOrder < rhs.LayerAndOrder ? -1 : 1;

                var sign = math.sign(lhs.SortingDistance - rhs.SortingDistance);
                if (sign != 0f)
                    return (int)sign;
                
                return lhs.Renderer.Index - rhs.Renderer.Index;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var comparer = new RenderItemComparer();
            
            inputDeps = Entities
                .ForEach((Entity e, 
                    ref Camera camera,
                    ref DynamicBuffer<RenderItem> renderItemBuffer) =>
            {
                var renderItemArray = renderItemBuffer.AsNativeArray();
                renderItemArray.Sort(comparer);
            }).Schedule(inputDeps);

            return inputDeps;
        }
    }
}
