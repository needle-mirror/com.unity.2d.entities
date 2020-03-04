using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.U2D.Entities.Physics
{
    internal static class Integrator
    {
        public static JobHandle ScheduleIntegrateJobs(ref PhysicsWorld world, JobHandle inputDeps)
        {
            return new IntegrateMotionsJob
            {
                BodyMotionData = world.DynamicsWorld.BodyMotionData,
                BodyMotionVelocity = world.DynamicsWorld.BodyMotionVelocity,
                Timestep = world.TimeStep,
                Gravity = world.Settings.Gravity

            }.Schedule(world.DynamicsWorld.BodyMotionCount, 64, inputDeps);
        }

        [BurstCompile]
        private struct IntegrateMotionsJob : IJobParallelFor
        {
            public NativeSlice<PhysicsBody.MotionData> BodyMotionData;
            public NativeSlice<PhysicsBody.MotionVelocity> BodyMotionVelocity;
            public float Timestep;
            public float2 Gravity;

            public void Execute(int index)
            {
                var motionData = BodyMotionData[index];
                var motionVelocity = BodyMotionVelocity[index];

                // Orientation.
                motionData.WorldPosition += motionVelocity.LinearVelocity * Timestep;
                motionData.WorldAngle += motionVelocity.AngularVelocity * Timestep;

                // Gravity.
                motionVelocity.LinearVelocity += Gravity * motionData.GravityScale * Timestep;

                // Damping.
                motionVelocity.LinearVelocity *= math.clamp(1.0f - motionData.LinearDamping * Timestep, 0.0f, 1.0f);
                motionVelocity.AngularVelocity *= math.clamp(1.0f - motionData.AngularDamping * Timestep, 0.0f, 1.0f);

                BodyMotionData[index] = motionData;
                BodyMotionVelocity[index] = motionVelocity;
            }
        }
    }
}
