using Unity.Entities;

using Camera = Unity.Tiny.Rendering.Camera;

namespace Unity.U2D.Entities
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal class SetupCameraBuffersSystem : ComponentSystem
    {
        private EntityQuery m_CamerasWithoutBufferQuery;
        private EntityQuery m_CamerasWithoutRenderInfoQuery;

        protected override void OnCreate()
        {
            m_CamerasWithoutBufferQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new []{ ComponentType.ReadOnly<Camera>() },
                None = new []{ ComponentType.ReadOnly<RenderItem>() }
            });

            m_CamerasWithoutRenderInfoQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<Camera>()},
                None = new[] {ComponentType.ReadOnly<DrawInstruction>()}
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_CamerasWithoutBufferQuery, typeof(RenderItem));
            EntityManager.AddComponent(m_CamerasWithoutRenderInfoQuery, typeof(DrawInstruction));
        }
    }
}
