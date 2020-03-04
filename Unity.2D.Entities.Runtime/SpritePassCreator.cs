using Unity.Collections;
using Unity.Entities;
using Unity.Tiny.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.U2D.Entities
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(SpriteRuntimeRendering))]
    [UpdateBefore(typeof(SubmitSystemGroup))]
    internal class SpritePassCreator : JobComponentSystem
    {
        private Entity m_GroupEntry = Entity.Null;
        private EntityQuery m_RenderPassQuery;

        protected override void OnCreate()
        {
            m_RenderPassQuery = GetEntityQuery(ComponentType.ReadOnly<RenderPass>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SetupRenderGroup();
            
            inputDeps.Complete();
            ConnectRenderersWithSpritePass();

            return inputDeps;
        }

        private void SetupRenderGroup()
        {
            if (m_GroupEntry != Entity.Null)
            {
                return;
            }

            var currentRenderPassEntries = m_RenderPassQuery.ToEntityArray(Allocator.TempJob);

            var cameraMask = new CameraMask { mask = ulong.MaxValue };
            var shadowMask = new ShadowMask { mask = ulong.MaxValue };

            var spriteBuildGroup = new BuildGroup
            {
                passTypes = RenderPassType.Sprites,
                cameraMask = cameraMask,
                shadowMask = shadowMask
            };

            m_GroupEntry = EntityManager.CreateEntity();
            EntityManager.AddComponent<RenderGroup>(m_GroupEntry);
            EntityManager.AddComponentData<BuildGroup>(m_GroupEntry, spriteBuildGroup);
            var groupTargetPasses = EntityManager.AddBuffer<RenderToPassesEntry>(m_GroupEntry);

            for (var i = 0; i < currentRenderPassEntries.Length; i++)
            {
                var renderPassEntry = currentRenderPassEntries[i];
                var renderPass = EntityManager.GetComponentData<RenderPass>(renderPassEntry);
                if (((uint) renderPass.passType & (uint) spriteBuildGroup.passTypes) == 0)
                {
                    continue;
                }

                groupTargetPasses.Add(new RenderToPassesEntry {e = renderPassEntry});
            }

            currentRenderPassEntries.Dispose();
        }

        private void ConnectRenderersWithSpritePass()
        {
            Entities
                .WithNone<RenderToPasses>()
                .WithAll<SpriteRenderer>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity) =>
                {
                    var renderToPassesEntry = new RenderToPasses { e = m_GroupEntry };
                    if (EntityManager.HasComponent<RenderToPasses>(entity))
                    {
                        unsafe
                        {
                            var oldValue = EntityManager.GetSharedComponentData<RenderToPasses>(entity);
                            if (UnsafeUtility.MemCmp(&oldValue, &renderToPassesEntry, sizeof(RenderToPasses)) != 0)
                            {
                                EntityManager.SetSharedComponentData(entity, renderToPassesEntry);
                            }
                        }
                    }
                    else
                    {
                        EntityManager.AddSharedComponentData(entity, renderToPassesEntry);
                    }
                }).Run();
        }
    }
}
