using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Tiny.Rendering;
using Unity.Transforms;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdateCameraMatricesSystem))]
    [UpdateAfter(typeof(SetupCameraBuffersSystem))]
    [UpdateAfter(typeof(UpdateWorldBoundsSystem))]
    [UpdateAfter(typeof(SpriteAtlasBarrier))]
    internal class SpriteRendererCullingSystem : JobComponentSystem
    {
        private EntityQuery m_SpriteRendererQuery;
        
        protected override void OnCreate()
        {
            m_SpriteRendererQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<SpriteRenderer>(), 
                    ComponentType.ReadOnly<WorldBounds>(), 
                    ComponentType.ReadOnly<LocalToWorld>(), 
                    ComponentType.ReadOnly<SortLayer>(),
                }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var renderItemsBuffer = GetBufferFromEntity<RenderItem>();
            var entityType = GetArchetypeChunkEntityType();
            var spriteRenderers = GetArchetypeChunkComponentType<SpriteRenderer>(true);
            var boundsType = GetArchetypeChunkComponentType<WorldBounds>(true);
            var transforms = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var sortLayers = GetArchetypeChunkComponentType<SortLayer>(true);
            var rendererChunks = m_SpriteRendererQuery.CreateArchetypeChunkArray(Allocator.TempJob);
                
            inputDeps = Entities
                .WithNativeDisableContainerSafetyRestriction(renderItemsBuffer)
                .WithReadOnly(entityType)
                .WithReadOnly(spriteRenderers)
                .WithReadOnly(boundsType)
                .WithReadOnly(transforms)
                .WithReadOnly(sortLayers)
                .WithDeallocateOnJobCompletion(rendererChunks)
                .ForEach((Entity entity,
                    ref Camera camera, 
                    ref CameraMatrices cameraMatrices, 
                    ref CameraSettings2D settings2D) =>
                {
                    var renderItemBuffer = renderItemsBuffer[entity]; 
                    renderItemBuffer.Clear();                
                    
                    for (var i = 0; i < rendererChunks.Length; i++)
                    {
                        var chunk = rendererChunks[i];
                        var entities = chunk.GetNativeArray(entityType);
                        var renderersArray = chunk.GetNativeArray(spriteRenderers);
                        var boundsArray = chunk.GetNativeArray(boundsType);
                        var transformArray = chunk.GetNativeArray(transforms);
                        var sortDataArray = chunk.GetNativeArray(sortLayers);
                        
                        for (var m = 0; m < chunk.Count; m++)
                        {
                            var renderer = renderersArray[m];
                            if (!IsValidSpriteRenderer(renderer))
                            {
                                continue; 
                            }
                            
                            var bounds = boundsArray[m];
                            var culled = Culling.IsCulled(ref bounds, ref cameraMatrices.frustum);
                            if (!culled)
                            {
                                var center = (bounds.c000 + bounds.c111) / 2.0f;
                                var sortingDistance = -math.dot(settings2D.customSortAxis, center);
                                
                                renderItemBuffer.Add(new RenderItem
                                {
                                    Renderer = entities[m],
                                    Type = RenderItemType.Sprite,
                                    Transform = transformArray[m].Value,
                                    LayerAndOrder = MergeLayerAndOrder(sortDataArray[m].Layer, sortDataArray[m].Order, -1f),
                                    SortingDistance = sortingDistance
                                });
                            } 
                        }
                    }                    
                }).Schedule(inputDeps);
            
            return inputDeps;
        }
        
        private static bool IsValidSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            return spriteRenderer.Sprite != Entity.Null && 
                spriteRenderer.Material != Entity.Null;
        }
            
        private static ulong MergeLayerAndOrder(short layer, short order, float z)
        {
            // Make sure negative values are capped as shorts
            var ulLayer = (ulong)(ushort)layer;
            var ulOrder = (ulong)(ushort)order;
                
            var unsignedZ = math.asuint(z);
                
            // Fix up twos complement for negative floats
            unsignedZ ^= (uint)((int)unsignedZ >> 31) >> 1; 
                
            // Pack and fixup signed values for sort
            var packed = ulLayer << 48 | ulOrder << 32 | (ulong)unsignedZ;
            return packed ^ 0x8000_8000_80000000ul; 
        }         
    }
}