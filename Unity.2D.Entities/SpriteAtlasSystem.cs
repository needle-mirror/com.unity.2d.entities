using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.U2D.Entities
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]    
    internal class SpriteAtlasBarrier : EntityCommandBufferSystem {}
    
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal class SpriteAtlasSystem : JobComponentSystem
    {
        private SpriteAtlasBarrier m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<SpriteAtlasBarrier>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
#if UNITY_EDITOR            
            // Added since the systems might run while the editor is compiling
            if (UnityEditor.EditorApplication.isCompiling)
            {
                return inputDeps; 
            }   
#endif            
            
            var cmdBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent();
            
            var spriteMeshes = GetBufferFromEntity<SpriteAtlasEntry>(true);
            var atlases = GetComponentDataFromEntity<SpriteAtlasTexture>(true);

            inputDeps = Entities
                .WithReadOnly(spriteMeshes)
                .WithReadOnly(atlases)
                .ForEach((
                    Entity entity,
                    int entityInQueryIndex,
                    in Sprite sprite) =>
            {
                // TODO consider attaching SpriteRenderData to SpriteRenderer instead
                var atlas = atlases[sprite.Atlas];
                cmdBuffer.AddComponent(entityInQueryIndex, entity, new SpriteRenderData
                {
                    Mesh = spriteMeshes[sprite.Atlas][sprite.Index].Value,
                    Texture = atlas.Texture
                });
            }).Schedule(inputDeps);
            
            m_Barrier.AddJobHandleForProducer(inputDeps);
            
            return inputDeps;
        }
    }
}
