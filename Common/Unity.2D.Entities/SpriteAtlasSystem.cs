using Unity.Entities;

namespace Unity.U2D.Entities
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]    
    internal class SpriteAtlasBarrier : EntityCommandBufferSystem {}
    
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal partial class SpriteAtlasSystem : SystemBase
    {
        private SpriteAtlasBarrier m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<SpriteAtlasBarrier>();
        }

        protected override void OnUpdate()
        {
#if UNITY_EDITOR            
            // Added since the systems might run while the editor is compiling
            if (UnityEditor.EditorApplication.isCompiling)
                return;   
#endif            
            
            var cmdBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            
            var spriteMeshes = GetBufferFromEntity<SpriteAtlasEntry>(true);
            var atlases = GetComponentDataFromEntity<SpriteAtlasTexture>(true);

            Dependency = Entities
                .WithName("AddSpriteRenderDataToAtlas")
                .WithReadOnly(spriteMeshes)
                .WithReadOnly(atlases)
                .WithNone<SpriteRenderData>()
                .ForEach((
                    int entityInQueryIndex,
                    in Entity entity,
                    in Sprite sprite) =>
                {
                    var atlas = atlases[sprite.Atlas];
                    cmdBuffer.AddComponent(entityInQueryIndex, entity, new SpriteRenderData
                    {
                        Mesh = spriteMeshes[sprite.Atlas][sprite.Index].Value,
                        Texture = atlas.Texture
                    });
                }).ScheduleParallel(Dependency);
            
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
}
