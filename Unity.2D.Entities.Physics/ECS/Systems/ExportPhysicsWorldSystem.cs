using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.U2D.Entities.Physics.PhysicsMath;

namespace Unity.U2D.Entities.Physics
{
    [UpdateAfter(typeof(StepPhysicsWorldSystem)), UpdateBefore(typeof(EndFramePhysicsSystem)), UpdateBefore(typeof(TransformSystemGroup))]
    public class ExportPhysicsWorldSystem : JobComponentSystem
    {
        public JobHandle FinalJobHandle { get; private set; }

        PhysicsWorldSystem m_PhysicsWorldSystem;
        StepPhysicsWorldSystem m_StepPhysicsWorldSystem;

        protected override void OnCreate()
        {
            FinalJobHandle = default;

            m_PhysicsWorldSystem = World.GetOrCreateSystem<PhysicsWorldSystem>();
            m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorldSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = JobHandle.CombineDependencies(inputDeps, m_StepPhysicsWorldSystem.FinalJobHandle);

            ref PhysicsWorld world = ref m_PhysicsWorldSystem.PhysicsWorld;

            var translationType = GetArchetypeChunkComponentType<Translation>();
            var rotationType = GetArchetypeChunkComponentType<Rotation>();
            var velocityType = GetArchetypeChunkComponentType<PhysicsVelocity>();

            handle = new ExportDynamicBodiesJob
            {
                BodyMotionData = world.DynamicsWorld.BodyMotionData,
                BodyMotionVelocity = world.DynamicsWorld.BodyMotionVelocity,

                TranslationType = translationType,
                RotationType = rotationType,
                VelocityType = velocityType

            }.Schedule(m_PhysicsWorldSystem.DynamicEntityGroup, handle);
            
            // Schedule phase callback.
            FinalJobHandle = m_PhysicsWorldSystem.Callbacks.ScheduleCallbacksForPhase(PhysicsCallbacks.Phase.PostExport, ref m_PhysicsWorldSystem.PhysicsWorld, handle);
            return FinalJobHandle;
        }

        [BurstCompile]
        internal struct ExportDynamicBodiesJob : IJobChunk
        {
            [ReadOnly] public NativeSlice<PhysicsBody.MotionData> BodyMotionData;
            [ReadOnly] public NativeSlice<PhysicsBody.MotionVelocity> BodyMotionVelocity;

            public ArchetypeChunkComponentType<Translation> TranslationType;
            public ArchetypeChunkComponentType<Rotation> RotationType;
            public ArchetypeChunkComponentType<PhysicsVelocity> VelocityType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkTranslations = chunk.GetNativeArray(TranslationType);
                var chunkRotations = chunk.GetNativeArray(RotationType);
                var chunkVelocities = chunk.GetNativeArray(VelocityType);

                for(int i = 0, bodyMotionIndex = firstEntityIndex; i < chunk.Count; ++i, ++bodyMotionIndex)
                {
                    var motionData = BodyMotionData[bodyMotionIndex];
                    var motionVelocity = BodyMotionVelocity[bodyMotionIndex];

                    var translation = motionData.WorldPosition - mul(float2x2.Rotate(motionData.WorldAngle), motionData.LocalCenterOfMass);
                    var rotation = QuaternionFromZRotation(motionData.WorldAngle);

                    chunkTranslations[i] = new Translation { Value = new float3(translation, 0.0f) };
                    chunkRotations[i] = new Rotation { Value = rotation };
                    chunkVelocities[i] = new PhysicsVelocity
                    {
                        Linear = motionVelocity.LinearVelocity,
                        Angular = motionVelocity.AngularVelocity
                    };
                }
            }
        }
    }
}
