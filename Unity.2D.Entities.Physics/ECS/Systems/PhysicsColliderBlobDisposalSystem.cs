using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.U2D.Entities.Physics
{
    // Handle disposing of Collider blobs when the entity has been destroyed
    // and the presence of a PhysicsColliderBlobOwner is detected.
    [UpdateBefore(typeof(PhysicsWorldSystem))]
    internal class PhysicsColliderBlobDisposalSystem : ComponentSystem
    {
        private EntityQuery m_DisposeColliderBlobsQuery;

        protected override void OnCreate()
        {
            m_DisposeColliderBlobsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PhysicsColliderBlobOwner>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<PhysicsColliderBlob>()
                }
            });
        }

        protected override void OnUpdate()
        {
            Entities
                .With(m_DisposeColliderBlobsQuery)
                .ForEach((Entity entity, ref PhysicsColliderBlobOwner colliderBlobOwner) =>
                {
                    var colliderBlob = colliderBlobOwner.Collider;
                    if (colliderBlob.IsCreated)
                    {
                        colliderBlob.Dispose();
                    }

                    EntityManager.RemoveComponent<PhysicsColliderBlobOwner>(entity);
                });
        }
    }
}
