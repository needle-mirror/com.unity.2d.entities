using Unity.Entities;

namespace Unity.U2D.Entities
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteAtlasBarrier))]
    internal class CleanupDrawCallSystem : ComponentSystem
    {
        private EntityQuery m_DrawCalls;
        
        protected override void OnCreate()
        {
            m_DrawCalls = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<DrawCall>()}
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(m_DrawCalls);
        }
    }
}