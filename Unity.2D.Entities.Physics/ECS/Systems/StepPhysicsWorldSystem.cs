using Unity.Entities;
using Unity.Jobs;

namespace Unity.U2D.Entities.Physics
{
    [UpdateAfter(typeof(PhysicsWorldSystem)), UpdateBefore(typeof(ExportPhysicsWorldSystem))]
    public class StepPhysicsWorldSystem : JobComponentSystem
    {
        public JobHandle FinalJobHandle { get; private set; }

        private PhysicsWorldSystem m_PhysicsWorldSystem;

        protected override void OnCreate()
        {
            FinalJobHandle = default;

            m_PhysicsWorldSystem = World.GetExistingSystem<PhysicsWorldSystem>();
        }

        protected override void OnDestroy()
        {
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = JobHandle.CombineDependencies(m_PhysicsWorldSystem.FinalJobHandle, inputDeps);

            // Simulate.
            FinalJobHandle = ScheduleSimulate(ref m_PhysicsWorldSystem.PhysicsWorld, m_PhysicsWorldSystem.Callbacks, handle);
            return FinalJobHandle;
        }

        private JobHandle ScheduleSimulate(ref PhysicsWorld physicsWorld, PhysicsCallbacks callbacks, JobHandle inputDeps)
        {
            // Execute phase callback.
            var handle = callbacks.ScheduleCallbacksForPhase(PhysicsCallbacks.Phase.PreStepSimulation, ref physicsWorld, inputDeps);

            // Do we have any dynamic bodies?
            if (physicsWorld.DynamicBodyCount > 0)
            {
                // Schedule integration.
                handle = Integrator.ScheduleIntegrateJobs(ref physicsWorld, handle);
            }

            // Schedule phase callback.
            return callbacks.ScheduleCallbacksForPhase(PhysicsCallbacks.Phase.PostIntegrate, ref physicsWorld, handle);
        }
    }
}
