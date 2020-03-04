﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.U2D.Entities.Physics.PhysicsMath;

namespace Unity.U2D.Entities.Physics
{
    public struct CollisionWorld : IQueryable, IDisposable
    {
        public Broadphase Broadphase;

        public NativeSlice<PhysicsBody> AllBodies => new NativeSlice<PhysicsBody>(m_PhysicsBodies, 0, BodyCount);
        public NativeSlice<PhysicsBody> StaticBodies => new NativeSlice<PhysicsBody>(m_PhysicsBodies, DynamicBodyCount, StaticBodyCount);
        public NativeSlice<PhysicsBody> DynamicBodies => new NativeSlice<PhysicsBody>(m_PhysicsBodies, 0, DynamicBodyCount);

        public int BodyCount => Broadphase.StaticBodyCount + Broadphase.DynamicBodyCount;
        public int StaticBodyCount => Broadphase.StaticBodyCount;
        public int DynamicBodyCount => Broadphase.DynamicBodyCount;

        private NativeArray<PhysicsBody> m_PhysicsBodies;

        public CollisionWorld(int staticBodyCount, int dynamicBodyCount)
        {
            m_PhysicsBodies = new NativeArray<PhysicsBody>(staticBodyCount + dynamicBodyCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Broadphase = new Broadphase(staticBodyCount, dynamicBodyCount);
        }

        internal void Reset(int staticBodyCount, int dynamicBodyCount)
        {
            Broadphase.Reset(staticBodyCount : staticBodyCount, dynamicBodyCount : dynamicBodyCount);
            SetCapacity(staticBodyCount + dynamicBodyCount);
        }

        private void SetCapacity(int bodyCount)
        {
            // Increase body storage if necessary
            if (m_PhysicsBodies.Length < bodyCount)
            {
                m_PhysicsBodies.Dispose();
                m_PhysicsBodies = new NativeArray<PhysicsBody>(bodyCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
        }

        public bool OverlapAabb(OverlapAabbInput input, NativeList<int> physicsBodyIndices)
        {
            return Broadphase.OverlapAabb(input, m_PhysicsBodies, physicsBodyIndices);
        }

        #region IQueryable

        public Aabb CalculateAabb()
        {
            return Broadphase.Domain;
        }

        public Aabb CalculateAabb(PhysicsTransform transform)
        {
            return mul(transform, Broadphase.Domain);
        }

        public bool OverlapPoint(OverlapPointInput input) => QueryWrappers.OverlapPoint(ref this, input);
        public bool OverlapPoint(OverlapPointInput input, out OverlapPointHit hit) => QueryWrappers.OverlapPoint(ref this, input, out hit);
        public bool OverlapPoint(OverlapPointInput input, ref NativeList<OverlapPointHit> allHits) => QueryWrappers.OverlapPoint(ref this, input, ref allHits);
        public bool OverlapPoint<T>(OverlapPointInput input, ref T collector) where T : struct, ICollector<OverlapPointHit>
        {
            return Broadphase.OverlapPoint(input, m_PhysicsBodies, ref collector);
        }

        // Check a collider against this body.
        public bool OverlapCollider(OverlapColliderInput input) => QueryWrappers.OverlapCollider(ref this, input);
        public bool OverlapCollider(OverlapColliderInput input, out OverlapColliderHit hit) => QueryWrappers.OverlapCollider(ref this, input, out hit);
        public bool OverlapCollider(OverlapColliderInput input, ref NativeList<OverlapColliderHit> allHits) => QueryWrappers.OverlapCollider(ref this, input, ref allHits);
        public bool OverlapCollider<T>(OverlapColliderInput input, ref T collector) where T : struct, ICollector<OverlapColliderHit>
        {
            return Broadphase.OverlapCollider(input, m_PhysicsBodies, ref collector);
        }

        public bool CastRay(RaycastInput input) => QueryWrappers.RayCast(ref this, input);
        public bool CastRay(RaycastInput input, out RaycastHit closestHit) => QueryWrappers.RayCast(ref this, input, out closestHit);
        public bool CastRay(RaycastInput input, ref NativeList<RaycastHit> allHits) => QueryWrappers.RayCast(ref this, input, ref allHits);
        public bool CastRay<T>(RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>
        {
            return Broadphase.CastRay(input, m_PhysicsBodies, ref collector);
        }

        public bool CastCollider(ColliderCastInput input) => QueryWrappers.ColliderCast(ref this, input);
        public bool CastCollider(ColliderCastInput input, out ColliderCastHit closestHit) => QueryWrappers.ColliderCast(ref this, input, out closestHit);
        public bool CastCollider(ColliderCastInput input, ref NativeList<ColliderCastHit> allHits) => QueryWrappers.ColliderCast(ref this, input, ref allHits);
        public bool CastCollider<T>(ColliderCastInput input, ref T collector) where T : struct, ICollector<ColliderCastHit>
        {
            return Broadphase.CastCollider(input, m_PhysicsBodies, ref collector);
        }

        public bool CalculateDistance(PointDistanceInput input) => QueryWrappers.CalculateDistance(ref this, input);
        public bool CalculateDistance(PointDistanceInput input, out DistanceHit closestHit) => QueryWrappers.CalculateDistance(ref this, input, out closestHit);
        public bool CalculateDistance(PointDistanceInput input, ref NativeList<DistanceHit> allHits) => QueryWrappers.CalculateDistance(ref this, input, ref allHits);
        public bool CalculateDistance<T>(PointDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
        {
            return Broadphase.CalculateDistance(input, m_PhysicsBodies, ref collector);
        }

        public bool CalculateDistance(ColliderDistanceInput input) => QueryWrappers.CalculateDistance(ref this, input);
        public bool CalculateDistance(ColliderDistanceInput input, out DistanceHit closestHit) => QueryWrappers.CalculateDistance(ref this, input, out closestHit);
        public bool CalculateDistance(ColliderDistanceInput input, ref NativeList<DistanceHit> allHits) => QueryWrappers.CalculateDistance(ref this, input, ref allHits);
        public bool CalculateDistance<T>(ColliderDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
        {
            return Broadphase.CalculateDistance(input, m_PhysicsBodies, ref collector);
        }

        #endregion

        #region Cloneable

        public CollisionWorld Clone()
        {
            return new CollisionWorld
            {
                m_PhysicsBodies = new NativeArray<PhysicsBody>(m_PhysicsBodies, Allocator.Persistent),
                Broadphase = Broadphase.Clone()
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {           
            if (m_PhysicsBodies.IsCreated)
            {
                m_PhysicsBodies.Dispose();
            }

            Broadphase.Dispose();
        }

        #endregion

        #region Jobs

        // Schedule a set of jobs to build the broadphase based on the given world.
        internal JobHandle ScheduleBuildBroadphaseJobs(ref PhysicsWorld world, NativeArray<int> buildStaticTree, JobHandle inputDeps)
        {
            return Broadphase.ScheduleBuildJobs(ref world, buildStaticTree, inputDeps);
        }

        // Synchronize the collision world with the dynamics world.
        internal void UpdateDynamicTree(ref PhysicsWorld world)
        {
            // Synchronize transforms
            for (int i = 0; i < world.DynamicsWorld.BodyMotionCount; i++)
            {
                UpdatePhysicsBodyTransformsJob.Execute(i, world.BodyMotionData, m_PhysicsBodies);
            }

            // Update broadphase
            var aabbMargin = PhysicsSettings.Constants.CollisionTolerance * 0.5f;
            Broadphase.BuildDynamicTree(world.DynamicBodies, world.BodyMotionData, world.BodyMotionVelocity, world.Settings.Gravity, world.TimeStep, aabbMargin);
        }

        // Schedule a set of jobs to synchronize the collision world with the dynamics world.
        internal JobHandle ScheduleUpdateDynamicTree(ref PhysicsWorld world, JobHandle inputDeps)
        {
            if (world.Settings.NumberOfThreadsHint <= 0)
            {
                return new UpdateDynamicLayerJob
                {
                    World = world,
                    TimeStep = world.TimeStep,
                    Gravity = world.Settings.Gravity

                }.Schedule(inputDeps);
            }

            // Synchronize transforms
            var handle = new UpdatePhysicsBodyTransformsJob
            {
                MotionDatas = world.BodyMotionData,
                PhysicsBodies = m_PhysicsBodies

            }.Schedule(world.BodyMotionData.Length, 32, inputDeps);

            // Update broadphase
            return Broadphase.ScheduleDynamicTreeBuildJobs(ref world, handle);
        }

        [BurstCompile]
        private struct UpdateDynamicLayerJob : IJob
        {
            public PhysicsWorld World;
            public float TimeStep;
            public float2 Gravity;

            public void Execute()
            {
                World.CollisionWorld.UpdateDynamicTree(ref World);
            }
        }

        [BurstCompile]
        private struct UpdatePhysicsBodyTransformsJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<PhysicsBody.MotionData> MotionDatas;
            public NativeSlice<PhysicsBody> PhysicsBodies;

            public void Execute(int index)
            {
                Execute(index, MotionDatas, PhysicsBodies);
            }

            internal static void Execute(int index, NativeSlice<PhysicsBody.MotionData> motionDatas, NativeSlice<PhysicsBody> physicsbodies)
            {
                var motionData = motionDatas[index];
                var physicsBody = physicsbodies[index];

                var rotation = float2x2.Rotate(motionData.WorldAngle);
                var translation = motionData.WorldPosition - mul(rotation, motionData.LocalCenterOfMass);

                physicsBody.WorldTransform = new PhysicsTransform
                {
                    Rotation = rotation,
                    Translation = translation
                };

                physicsbodies[index] = physicsBody;
            }
        }

        #endregion
    }
}
