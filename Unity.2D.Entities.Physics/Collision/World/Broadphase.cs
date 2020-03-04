using System;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.U2D.Entities.Physics.PhysicsMath;
using static Unity.U2D.Entities.Physics.BoundingVolumeHierarchy;

namespace Unity.U2D.Entities.Physics
{
    public struct Broadphase : IDisposable
    {
        private Tree m_StaticTree;
        private Tree m_DynamicTree;

        public Tree StaticTree => m_StaticTree;
        public Tree DynamicTree => m_DynamicTree;
        public Aabb Domain => Aabb.Union(m_StaticTree.BoundingVolumeHierarchy.Domain, m_DynamicTree.BoundingVolumeHierarchy.Domain);
        public int StaticBodyCount => m_StaticTree.BodyCount;
        public int DynamicBodyCount => m_DynamicTree.BodyCount;

        public Broadphase(int staticBodyCount, int dynamicBodyCount)
        {
            m_StaticTree = new Tree(staticBodyCount);
            m_DynamicTree = new Tree(dynamicBodyCount);
        }

        public void Reset(int staticBodyCount, int dynamicBodyCount)
        {
            m_StaticTree.Reset(staticBodyCount);
            m_DynamicTree.Reset(dynamicBodyCount);
        }

        #region Queries

        public bool OverlapAabb(OverlapAabbInput input, NativeSlice<PhysicsBody> rigidBodies, NativeList<int> physicsBodyIndices)
        {
            if (input.Filter.IsEmpty)
                return false;

            var hitsBefore = physicsBodyIndices.Length;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);
            var leafCollector = new PhysicsBodyOverlapsCollector { PhysicsBodyIndices = physicsBodyIndices };

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            m_StaticTree.BoundingVolumeHierarchy.AabbOverlap(input, ref leafProcessor, ref leafCollector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            m_DynamicTree.BoundingVolumeHierarchy.AabbOverlap(input, ref leafProcessor, ref leafCollector);

            return physicsBodyIndices.Length > hitsBefore;
        }

        public bool OverlapPoint<T>(OverlapPointInput input, NativeSlice<PhysicsBody> rigidBodies, ref T collector)
            where T : struct, ICollector<OverlapPointHit>
        {
            if (input.Filter.IsEmpty)
                return false;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            bool hasHit = m_StaticTree.BoundingVolumeHierarchy.OverlapPoint(input, ref leafProcessor, ref collector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            hasHit |= m_DynamicTree.BoundingVolumeHierarchy.OverlapPoint(input, ref leafProcessor, ref collector);
            return hasHit;
        }

        public bool OverlapCollider<T>(OverlapColliderInput input, NativeSlice<PhysicsBody> rigidBodies, ref T collector)
            where T : struct, ICollector<OverlapColliderHit>
        {
            if (input.Filter.IsEmpty)
                return false;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            bool hasHit = m_StaticTree.BoundingVolumeHierarchy.OverlapCollider(input, ref leafProcessor, ref collector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            hasHit |= m_DynamicTree.BoundingVolumeHierarchy.OverlapCollider(input, ref leafProcessor, ref collector);
            return hasHit;
        }

        public bool CastRay<T>(RaycastInput input, NativeSlice<PhysicsBody> rigidBodies, ref T collector)
            where T : struct, ICollector<RaycastHit>
        {
            if (input.Filter.IsEmpty)
                return false;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            bool hasHit = m_StaticTree.BoundingVolumeHierarchy.Raycast(input, ref leafProcessor, ref collector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            hasHit |= m_DynamicTree.BoundingVolumeHierarchy.Raycast(input, ref leafProcessor, ref collector);
            return hasHit;
        }

        public unsafe bool CastCollider<T>(ColliderCastInput input, NativeSlice<PhysicsBody> rigidBodies, ref T collector)
            where T : struct, ICollector<ColliderCastHit>
        {
            PhysicsAssert.IsTrue(input.Collider.IsCreated);

            if (input.Collider.Value.Filter.IsEmpty)
                return false;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            bool hasHit = m_StaticTree.BoundingVolumeHierarchy.ColliderCast(input, ref leafProcessor, ref collector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            hasHit |= m_DynamicTree.BoundingVolumeHierarchy.ColliderCast(input, ref leafProcessor, ref collector);
            return hasHit;
        }

        public bool CalculateDistance<T>(PointDistanceInput input, NativeSlice<PhysicsBody> rigidBodies, ref T collector)
            where T : struct, ICollector<DistanceHit>
        {
            if (input.Filter.IsEmpty)
                return false;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            bool hasHit = m_StaticTree.BoundingVolumeHierarchy.Distance(input, ref leafProcessor, ref collector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            hasHit |= m_DynamicTree.BoundingVolumeHierarchy.Distance(input, ref leafProcessor, ref collector);
            return hasHit;
        }

        public unsafe bool CalculateDistance<T>(ColliderDistanceInput input, NativeSlice<PhysicsBody> rigidBodies, ref T collector)
            where T : struct, ICollector<DistanceHit>
        {
            PhysicsAssert.IsTrue(input.Collider.IsCreated);

            if (input.Collider.Value.Filter.IsEmpty)
                return false;

            var leafProcessor = new BvhLeafProcessor(rigidBodies);

            // Offset to Static bodies.
            leafProcessor.BasePhysicsBodyIndex = m_DynamicTree.BodyCount;
            bool hasHit = m_StaticTree.BoundingVolumeHierarchy.Distance(input, ref leafProcessor, ref collector);

            // No offset to Dynamic bodies.
            leafProcessor.BasePhysicsBodyIndex = 0;
            hasHit |= m_DynamicTree.BoundingVolumeHierarchy.Distance(input, ref leafProcessor, ref collector);
            return hasHit;
        }

        internal struct PhysicsBodyOverlapsCollector : IOverlapCollector
        {
            public NativeList<int> PhysicsBodyIndices;

            public unsafe void AddPhysicsBodyIndices(int* indices, int count)
            {
                PhysicsBodyIndices.AddRange(indices, count);
            }

            public unsafe void AddColliderKeys(ColliderKey* keys, int count)
            {
                throw new NotSupportedException();
            }

            public void PushCompositeCollider(ColliderKeyPath compositeKey)
            {
                throw new NotSupportedException();
            }

            public void PopCompositeCollider(uint numCompositeKeyBits)
            {
                throw new NotSupportedException();
            }
        }

        internal struct BvhLeafProcessor :
            BoundingVolumeHierarchy.IRaycastLeafProcessor,
            BoundingVolumeHierarchy.IColliderCastLeafProcessor,
            BoundingVolumeHierarchy.IPointOverlapLeafProcessor,
            BoundingVolumeHierarchy.IColliderOverlapLeafProcessor,
            BoundingVolumeHierarchy.IColliderDistanceLeafProcessor,
            BoundingVolumeHierarchy.IPointDistanceLeafProcessor,
            BoundingVolumeHierarchy.IAabbOverlapLeafProcessor
        {
            private readonly NativeSlice<PhysicsBody> m_Bodies;
            public int BasePhysicsBodyIndex;

            public BvhLeafProcessor(NativeSlice<PhysicsBody> bodies)
            {
                m_Bodies = bodies;
                BasePhysicsBodyIndex = 0;
            }

            public bool AabbOverlap(int physicsBodyIndex, ref NativeList<int> allHits)
            {
                allHits.Add(BasePhysicsBodyIndex + physicsBodyIndex);
                return true;
            }

            public bool RayLeaf<T>(RaycastInput input, int physicsBodyIndex, ref T collector) where T : struct, ICollector<RaycastHit>
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                PhysicsBody body = m_Bodies[physicsBodyIndex];

                var worldFromBody = body.WorldTransform;

                // Transform the ray into body space
                RaycastInput inputLs = input;
                {
                    PhysicsTransform bodyFromWorld = inverse(worldFromBody);
                    inputLs.Start = mul(bodyFromWorld, input.Start);
                    inputLs.End = mul(bodyFromWorld, input.End);
                    inputLs.QueryContext = new QueryContext(physicsBodyIndex, body.Entity, worldFromBody);
                }

                return body.CastRay(inputLs, ref collector);
            }

            public unsafe bool ColliderCastLeaf<T>(ColliderCastInput input, int physicsBodyIndex, ref T collector)
                where T : struct, ICollector<ColliderCastHit>
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                PhysicsBody body = m_Bodies[physicsBodyIndex];

                // Transform the input into body space
                var worldFromBody = body.WorldTransform;
                PhysicsTransform bodyFromWorld = inverse(worldFromBody);
                var inputLs = new ColliderCastInput
                {
                    Collider = input.Collider,
                    Ignore = input.Ignore,
                    Rotation = math.mul(math.inverse(body.WorldTransform.Rotation), input.Rotation),
                    Start = mul(bodyFromWorld, input.Start),
                    End = mul(bodyFromWorld, input.End),
                    QueryContext = new QueryContext(physicsBodyIndex, body.Entity, worldFromBody)
                };

                return body.CastCollider(inputLs, ref collector);
            }

            public bool PointLeaf<T>(OverlapPointInput input, int physicsBodyIndex, ref T collector) where T : struct, ICollector<OverlapPointHit>
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                PhysicsBody body = m_Bodies[physicsBodyIndex];

                var worldFromBody = body.WorldTransform;

                // Transform the ray into body space
                OverlapPointInput inputLs = input;
                {
                    PhysicsTransform bodyFromWorld = inverse(worldFromBody);
                    inputLs.Position = mul(bodyFromWorld, input.Position);
                    inputLs.QueryContext = new QueryContext(physicsBodyIndex, body.Entity, worldFromBody);
                }

                return body.OverlapPoint(inputLs, ref collector);
            }

            public unsafe bool ColliderLeaf<T>(OverlapColliderInput input, int physicsBodyIndex, ref T collector) where T : struct, ICollector<OverlapColliderHit>
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                PhysicsBody body = m_Bodies[physicsBodyIndex];

                var worldFromBody = body.WorldTransform;

                // Transform the ray into body space
                OverlapColliderInput inputLs = input;
                {
                    PhysicsTransform bodyFromWorld = inverse(worldFromBody);
                    inputLs.Transform = mul(bodyFromWorld, input.Transform);
                    inputLs.QueryContext = new QueryContext(physicsBodyIndex, body.Entity, worldFromBody);
                }

                return body.OverlapCollider(inputLs, ref collector);
            }

            public bool DistanceLeaf<T>(PointDistanceInput input, int physicsBodyIndex, ref T collector)
                where T : struct, ICollector<DistanceHit>
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                PhysicsBody body = m_Bodies[physicsBodyIndex];

                // Transform the input into body space
                PhysicsTransform worldFromBody = body.WorldTransform;
                PhysicsTransform bodyFromWorld = inverse(worldFromBody);
                PointDistanceInput inputLs = new PointDistanceInput
                {
                    Position = mul(bodyFromWorld, input.Position),
                    MaxDistance = input.MaxDistance,
                    Filter = input.Filter,
                    QueryContext = new QueryContext(physicsBodyIndex, body.Entity, worldFromBody)
                };

                return body.CalculateDistance(inputLs, ref collector);
            }

            public unsafe bool DistanceLeaf<T>(ColliderDistanceInput input, int physicsBodyIndex, ref T collector)
                where T : struct, ICollector<DistanceHit>
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                if (physicsBodyIndex > m_Bodies.Length)
                {
                }
                PhysicsBody body = m_Bodies[physicsBodyIndex];

                // Transform the input into body space
                PhysicsTransform worldFromBody = body.WorldTransform;
                PhysicsTransform bodyFromWorld = inverse(worldFromBody);
                ColliderDistanceInput inputLs = new ColliderDistanceInput
                {
                    Collider = input.Collider,
                    Transform = new PhysicsTransform
                    {
                        Translation = mul(bodyFromWorld, input.Transform.Translation),
                        Rotation = math.mul(math.inverse(body.WorldTransform.Rotation), input.Transform.Rotation)
                    },
                    MaxDistance = input.MaxDistance,
                    QueryContext = new QueryContext(physicsBodyIndex, body.Entity, worldFromBody)
                };

                return body.CalculateDistance(inputLs, ref collector);
            }

            public unsafe void AabbLeaf<T>(OverlapAabbInput input, int physicsBodyIndex, ref T collector)
                where T : struct, IOverlapCollector
            {
                physicsBodyIndex += BasePhysicsBodyIndex;
                PhysicsBody body = m_Bodies[physicsBodyIndex];
                if (body.Collider.IsCreated && CollisionFilter.IsCollisionEnabled(input.Filter, body.Collider.Value.Filter))
                {
                    collector.AddPhysicsBodyIndices(&physicsBodyIndex, 1);
                }
            }
        }

        #endregion

        #region Cloneable

        public Broadphase Clone()
        {
            return new Broadphase
            {
                m_StaticTree = m_StaticTree.Clone(),
                m_DynamicTree = m_DynamicTree.Clone()
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_StaticTree.Dispose();
            m_DynamicTree.Dispose();
        }

        #endregion

        #region Build

        /// <summary>
        /// Build the broadphase based on the given world.
        /// </summary>
        public void Build(
            NativeSlice<PhysicsBody> staticBodies, NativeSlice<PhysicsBody> dynamicBodies,
            NativeSlice<PhysicsBody.MotionData> motionDatas, NativeSlice<PhysicsBody.MotionVelocity> motionVelocities,
            float collisionTolerance,
            float timeStep,
            float2 gravity,
            bool buildStaticTree = true)
        {
            float aabbMargin = collisionTolerance * 0.5f; // each body contributes half

            if (buildStaticTree)
            {
                m_StaticTree.Reset(staticBodies.Length);
                BuildStaticTree(staticBodies, aabbMargin);
            }

            m_DynamicTree.Reset(dynamicBodies.Length);
            BuildDynamicTree(dynamicBodies, motionDatas, motionVelocities, gravity, timeStep, aabbMargin);
        }

        /// <summary>
        /// Build the static tree of the broadphase based on the given array of rigid bodies.
        /// </summary>
        public void BuildStaticTree(NativeSlice<PhysicsBody> staticBodies, float aabbMargin)
        {
            PhysicsAssert.IsTrue(staticBodies.Length == m_StaticTree.BodyCount);

            if (staticBodies.Length == 0)
            {
                return;
            }

            // Read bodies
            var aabbs = new NativeArray<Aabb>(staticBodies.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var points = new NativeArray<PointAndIndex>(staticBodies.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < staticBodies.Length; i++)
            {
                PrepareStaticBodyDataJob.Execute(i, aabbMargin, staticBodies, aabbs, points, m_StaticTree.BodyFilters);
            }

            // Build tree
            m_StaticTree.BoundingVolumeHierarchy.Build(points, aabbs, out int nodeCount);

            // Build node filters
            m_StaticTree.BoundingVolumeHierarchy.BuildCombinedCollisionFilter(m_StaticTree.BodyFilters, 1, nodeCount - 1);
        }

        /// <summary>
        /// Build the dynamic tree of the broadphase based on the given array of rigid bodies and motions.
        /// </summary>
        public void BuildDynamicTree(
            NativeSlice<PhysicsBody> dynamicBodies,
            NativeSlice<PhysicsBody.MotionData> motionDatas, NativeSlice<PhysicsBody.MotionVelocity> motionVelocities,
            float2 gravity, float timeStep, float aabbMargin)
        {
            PhysicsAssert.IsTrue(dynamicBodies.Length == m_DynamicTree.BodyCount);

            if (dynamicBodies.Length == 0)
            {
                return;
            }

            // Read bodies
            var aabbs = new NativeArray<Aabb>(dynamicBodies.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var points = new NativeArray<PointAndIndex>(dynamicBodies.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < dynamicBodies.Length; i++)
            {
                PrepareDynamicBodyDataJob.Execute(i, aabbMargin, gravity, timeStep, dynamicBodies, motionDatas, motionVelocities, aabbs, points, m_DynamicTree.BodyFilters);
            }

            // Build tree
            m_DynamicTree.BoundingVolumeHierarchy.Build(points, aabbs, out int nodeCount);

            // Build node filters
            m_DynamicTree.BoundingVolumeHierarchy.BuildCombinedCollisionFilter(m_DynamicTree.BodyFilters, 1, nodeCount - 1);
        }

        #endregion

        #region Tree

        public struct Tree : IDisposable
        {
            public NativeArray<Node> Nodes; // The nodes of the bounding volume
            public NativeArray<CollisionFilter> NodeFilters; // The collision filter for each node (a union of all its children)
            public NativeArray<CollisionFilter> BodyFilters; // A copy of the collision filter of each body
            internal NativeArray<Builder.Range> Ranges; // Used during building
            internal NativeArray<int> BranchCount; // Used during building

            public BoundingVolumeHierarchy BoundingVolumeHierarchy => new BoundingVolumeHierarchy(Nodes, NodeFilters);

            public int BodyCount => BodyFilters.Length;

            public Tree(int bodyCount)
            {
                this = default;
                SetCapacity(bodyCount);
                Ranges = new NativeArray<BoundingVolumeHierarchy.Builder.Range>(
                    BoundingVolumeHierarchy.Constants.MaxNumTreeBranches, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                BranchCount = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            }

            public void Reset(int bodyCount)
            {
                if (bodyCount != BodyFilters.Length)
                {
                    SetCapacity(bodyCount);
                }
            }

            private void SetCapacity(int bodyCount)
            {
                var nodeCount = bodyCount + BoundingVolumeHierarchy.Constants.MaxNumTreeBranches;

                if (Nodes.IsCreated)
                {
                    Nodes.Dispose();
                }
                Nodes = new NativeArray<BoundingVolumeHierarchy.Node>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)
                {
                    // Always initialize first 2 nodes as empty, to gracefully return from queries on an empty tree
                    [0] = BoundingVolumeHierarchy.Node.Empty,
                    [1] = BoundingVolumeHierarchy.Node.Empty
                };

                if (NodeFilters.IsCreated)
                {
                    NodeFilters.Dispose();
                }
                NodeFilters = new NativeArray<CollisionFilter>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)
                {
                    // All queries should descend past these special root nodes
                    [0] = CollisionFilter.Default,
                    [1] = CollisionFilter.Default
                };

                if (BodyFilters.IsCreated)
                {
                    BodyFilters.Dispose();
                }
                BodyFilters = new NativeArray<CollisionFilter>(bodyCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            public Tree Clone()
            {
                return new Tree
                {
                    Nodes = new NativeArray<BoundingVolumeHierarchy.Node>(Nodes, Allocator.Persistent),
                    NodeFilters = new NativeArray<CollisionFilter>(NodeFilters, Allocator.Persistent),
                    BodyFilters = new NativeArray<CollisionFilter>(BodyFilters, Allocator.Persistent),
                    Ranges = new NativeArray<BoundingVolumeHierarchy.Builder.Range>(Ranges, Allocator.Persistent),
                    BranchCount = new NativeArray<int>(BranchCount, Allocator.Persistent)
                };
            }

            public void Dispose()
            {
                if (Nodes.IsCreated)
                {
                    Nodes.Dispose();
                }

                if (NodeFilters.IsCreated)
                {
                    NodeFilters.Dispose();
                }

                if (BodyFilters.IsCreated)
                {
                    BodyFilters.Dispose();
                }

                if (Ranges.IsCreated)
                {
                    Ranges.Dispose();
                }

                if (BranchCount.IsCreated)
                {
                    BranchCount.Dispose();
                }
            }
        }

        #endregion

        #region Tree Build

        internal JobHandle ScheduleBuildJobs(ref PhysicsWorld world, NativeArray<int> buildStaticTree, JobHandle inputDeps)
        {
            if (world.Settings.NumberOfThreadsHint <= 0)
            {
                return new BuildBroadphaseJob
                {
                    StaticBodies = world.StaticBodies,
                    DynamicBodies = world.DynamicBodies,
                    MotionDatas = world.BodyMotionData,
                    MotionVelocities = world.BodyMotionVelocity,
                    CollisionTolerance = PhysicsSettings.Constants.CollisionTolerance,
                    TimeStep = world.TimeStep,
                    Gravity = world.Settings.Gravity,
                    BuildStaticTree = buildStaticTree,
                    Broadphase = this

                }.Schedule(inputDeps);
            }

            return JobHandle.CombineDependencies(
                ScheduleStaticTreeBuildJobs(ref world, buildStaticTree, inputDeps),
                ScheduleDynamicTreeBuildJobs(ref world, inputDeps));        
        }

        /// <summary>
        /// Schedule a set of jobs to build the static tree of the broadphase based on the given world.
        /// </summary>
        private unsafe JobHandle ScheduleStaticTreeBuildJobs(
            ref PhysicsWorld world,
            NativeArray<int> shouldDoWork,
            JobHandle inputDeps)
        {
            PhysicsAssert.IsTrue(world.StaticBodyCount == m_StaticTree.BodyCount);
            
            if (world.StaticBodyCount == 0)
                return inputDeps;

            var aabbs = new NativeArray<Aabb>(world.StaticBodyCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var points = new NativeArray<PointAndIndex>(world.StaticBodyCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var staticBodyCountArray = new NativeArray<int>(1, Allocator.TempJob);
            JobHandle handle = new PrepareStaticBodyCountJob
            {
                StaticBodyCount = world.StaticBodyCount,
                BuildStaticTree = shouldDoWork,
                StaticBodyCountArray = staticBodyCountArray

            }.Schedule(inputDeps);

            var staticBodyDataJobHandle = new PrepareStaticBodyDataJob
            {
                PhysicsBodies = world.StaticBodies,
                Aabbs = aabbs,
                Points = points,
                FiltersOut = m_StaticTree.BodyFilters,
                AabbMargin = PhysicsSettings.Constants.CollisionTolerance * 0.5f, // each body contributes half

            }.ScheduleUnsafeIndex0(staticBodyCountArray, 32, handle);

            handle = JobHandle.CombineDependencies(staticBodyDataJobHandle, staticBodyCountArray.Dispose(handle));

            return m_StaticTree.BoundingVolumeHierarchy.ScheduleBuildJobs(
                points, aabbs, m_StaticTree.BodyFilters, shouldDoWork, world.Settings.NumberOfThreadsHint, handle,
                m_StaticTree.Nodes.Length, m_StaticTree.Ranges, m_StaticTree.BranchCount);
        }

        internal JobHandle ScheduleDynamicTreeBuildJobs(ref PhysicsWorld world, JobHandle inputDeps)
        {
            PhysicsAssert.IsTrue(world.DynamicBodyCount == m_DynamicTree.BodyCount);
            if (world.DynamicBodyCount == 0)
            {
                return inputDeps;
            }

            var aabbs = new NativeArray<Aabb>(world.DynamicBodyCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var points = new NativeArray<PointAndIndex>(world.DynamicBodyCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var handle = new PrepareDynamicBodyDataJob
            {
                PhysicsBodies = world.DynamicBodies,
                MotionVelocities = world.BodyMotionVelocity,
                MotionDatas = world.BodyMotionData,
                Aabbs = aabbs,
                Points = points,
                FiltersOut = m_DynamicTree.BodyFilters,
                AabbMargin = PhysicsSettings.Constants.CollisionTolerance * 0.5f, // each body contributes half
                TimeStep = world.TimeStep,
                Gravity = world.Settings.Gravity

            }.Schedule(world.DynamicBodyCount, 32, inputDeps);

            var shouldDoWork = new NativeArray<int>(1, Allocator.TempJob);
            shouldDoWork[0] = 1;

            handle = m_DynamicTree.BoundingVolumeHierarchy.ScheduleBuildJobs(
                points, aabbs, m_DynamicTree.BodyFilters, shouldDoWork, world.Settings.NumberOfThreadsHint, handle,
                m_DynamicTree.Nodes.Length, m_DynamicTree.Ranges, m_DynamicTree.BranchCount);

            return shouldDoWork.Dispose(handle);
        }

        [BurstCompile]
        private struct PrepareStaticBodyCountJob : IJob
        {
            public int StaticBodyCount;
            public NativeArray<int> BuildStaticTree;
            public NativeArray<int> StaticBodyCountArray;

            public void Execute()
            {
                if (BuildStaticTree[0] == 1)
                {
                    StaticBodyCountArray[0] = StaticBodyCount;
                }
                else
                {
                    StaticBodyCountArray[0] = 0;
                }
            }
        }

        // Reads broadphase data from static rigid bodies
        [BurstCompile]
        private struct PrepareStaticBodyDataJob : IJobParallelForDefer
        {
            [ReadOnly] public NativeSlice<PhysicsBody> PhysicsBodies;
            [ReadOnly] public float AabbMargin;

            public NativeArray<Aabb> Aabbs;
            public NativeArray<PointAndIndex> Points;
            public NativeArray<CollisionFilter> FiltersOut;

            public unsafe void Execute(int index)
            {
                Execute(index, AabbMargin, PhysicsBodies, Aabbs, Points, FiltersOut);
            }

            internal static unsafe void Execute(
                int index,
                float aabbMargin,
                NativeSlice<PhysicsBody> physicsBodies,
                NativeArray<Aabb> aabbs,
                NativeArray<PointAndIndex> points,
                NativeArray<CollisionFilter> filtersOut)
            {
                var physicsBody = physicsBodies[index];

                Aabb aabb;
                if (physicsBody.Collider.IsCreated)
                {
                    aabb = physicsBody.Collider.Value.CalculateAabb(physicsBody.WorldTransform);
                    aabb.Inflate(aabbMargin);

                    filtersOut[index] = physicsBodies[index].Collider.Value.Filter;
                }
                else
                {
                    aabb.Min = aabb.Max = physicsBody.WorldTransform.Translation;

                    filtersOut[index] = CollisionFilter.Default;
                }

                aabbs[index] = aabb;
                points[index] = new BoundingVolumeHierarchy.PointAndIndex
                {
                    Position = aabb.Center,
                    Index = index
                };
            }
        }


        // Reads broadphase data from dynamic rigid bodies
        [BurstCompile]
        private struct PrepareDynamicBodyDataJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<PhysicsBody> PhysicsBodies;
            [ReadOnly] public NativeSlice<PhysicsBody.MotionVelocity> MotionVelocities;
            [ReadOnly] public NativeSlice<PhysicsBody.MotionData> MotionDatas;
            [ReadOnly] public float AabbMargin;
            [ReadOnly] public float2 Gravity;
            [ReadOnly] public float TimeStep;

            public NativeArray<PointAndIndex> Points;
            public NativeArray<Aabb> Aabbs;
            public NativeArray<CollisionFilter> FiltersOut;

            public unsafe void Execute(int index)
            {
                Execute(index, AabbMargin, Gravity, TimeStep, PhysicsBodies, MotionDatas, MotionVelocities, Aabbs, Points, FiltersOut);
            }

            internal static unsafe void Execute(int index, float aabbMargin, float2 gravity, float timeStep,
                NativeSlice<PhysicsBody> physicsBodies, NativeSlice<PhysicsBody.MotionData> bodyMotionData, NativeSlice<PhysicsBody.MotionVelocity> bodyMotionVelocity,
                NativeArray<Aabb> aabbs, NativeArray<PointAndIndex> points, NativeArray<CollisionFilter> filtersOut)
            {
                PhysicsBody body = physicsBodies[index];

                Aabb aabb;
                if (body.Collider.IsCreated)
                {
                    var motionData = bodyMotionData[index];
                    var motionVelocity = bodyMotionVelocity[index];

                    // Apply gravity only on a copy to get proper expansion for the AABB,
                    // actual applying of gravity will be done later in the physics step
                    motionVelocity.LinearVelocity += gravity * timeStep * motionData.GravityScale;
                    var expansion = motionVelocity.CalculateExpansion(timeStep);

                    // Inflate the collider AABB by the body motion.
                    aabb = expansion.ExpandAabb(body.Collider.Value.CalculateAabb(body.WorldTransform));
                    aabb.Inflate(aabbMargin);

                    filtersOut[index] = body.Collider.Value.Filter;
                }
                else
                {
                    aabb.Min = aabb.Max = body.WorldTransform.Translation;

                    filtersOut[index] = CollisionFilter.Zero;
                }

                aabbs[index] = aabb;
                points[index] = new BoundingVolumeHierarchy.PointAndIndex
                {
                    Position = aabb.Center,
                    Index = index
                };
            }
        }

        // Builds the broadphase in a single job.
        [BurstCompile]
        private struct BuildBroadphaseJob : IJob
        {
            [ReadOnly] public NativeSlice<PhysicsBody> StaticBodies;
            [ReadOnly] public NativeSlice<PhysicsBody> DynamicBodies;
            [ReadOnly] public NativeSlice<PhysicsBody.MotionData> MotionDatas;
            [ReadOnly] public NativeSlice<PhysicsBody.MotionVelocity> MotionVelocities;
            [ReadOnly] public float CollisionTolerance;
            [ReadOnly] public float TimeStep;
            [ReadOnly] public float2 Gravity;
            [ReadOnly] public NativeArray<int> BuildStaticTree;

            public Broadphase Broadphase;

            public void Execute()
            {
                Broadphase.Build(StaticBodies, DynamicBodies, MotionDatas, MotionVelocities, CollisionTolerance, TimeStep, Gravity, BuildStaticTree[0] == 1);
            }
        }

        #endregion
    }
}
