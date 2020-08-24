#if UNITY_TINY || UNITY_DOTSRUNTIME

using Unity.Entities;
using Unity.Platforms;

namespace Unity.U2D.Entities
{
    [UpdateAfter(typeof(SpriteMeshSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal class SpriteRendererCacheSystem : ResumableSystemBase
    {
        protected override void OnSuspendResume(object sender, SuspendResumeEvent evt)
        {
            if (!evt.Suspend)
                return;
                
            var cacheDataQuery = GetEntityQuery(ComponentType.ReadOnly<SpriteRenderer>(), ComponentType.ReadWrite<SpriteMeshCacheData>());
            EntityManager.RemoveComponent(cacheDataQuery, ComponentType.ReadWrite<SpriteMeshCacheData>());
        }
        
        protected override void OnUpdate()
        {
            var barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            var cmd = barrier.CreateCommandBuffer();
            var em = EntityManager;

            Entities
                .WithNone<SpriteMeshCacheData>()
                .ForEach((Entity e, in SpriteRenderer renderer) =>
                {
                    if (TinyUtils.ExtractCacheData(em, renderer.Sprite, out var cacheData))
                        cmd.AddComponent(e, cacheData);
                }).Run();

            Entities
                .WithChangeFilter<SpriteRenderer>()
                .ForEach((Entity e, in SpriteMeshCacheData cacheData, in SpriteRenderer renderer) =>
                {
                    var hash = new Hash128((uint)renderer.Sprite.Index, (uint)renderer.Sprite.Version, 0, 0);
                    if (cacheData.Hash != hash)
                    {
                        if (TinyUtils.ExtractCacheData(em, renderer.Sprite, out var newCacheData))
                            cmd.SetComponent(e, newCacheData);
                    }
                }).Run();
            
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}

#endif // UNITY_TINY || UNITY_DOTSRUNTIME