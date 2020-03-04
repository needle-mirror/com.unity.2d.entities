using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.U2D.Entities.Physics
{
    [UpdateBefore(typeof(StepPhysicsWorldSystem))]
    public class PhysicsWorldSystem : JobComponentSystem
    {
        public PhysicsWorld PhysicsWorld;

        public JobHandle FinalJobHandle { get; private set; }

        public EntityQuery StaticEntityGroup { get; private set; }
        public EntityQuery DynamicEntityGroup { get; private set; }

        // A look-up from an Entity to a Physics Body Index.
        public struct EntityToPhysicsBodyIndex : IDisposable
        {
            public NativeHashMap<Entity, int> Lookup;

            // Reset the look-up and increase its capacity if required.
            // NOTE: We'll create one if it's not around.
            internal void Reset(int totalBodyCount)
            {
                totalBodyCount = math.max(1, totalBodyCount);

                if (Lookup.IsCreated)
                {
                    Lookup.Clear();
                    if (Lookup.Capacity < totalBodyCount)
                    {
                        Lookup.Capacity = totalBodyCount;
                    }
                    return;
                }

                Lookup = new NativeHashMap<Entity, int>(totalBodyCount, Allocator.Persistent);
            }

            public void Dispose()
            {
                if (Lookup.IsCreated)
                    Lookup.Dispose();
            }
        }
        public EntityToPhysicsBodyIndex EntityToPhysicsBody => m_EntityToPhysicsBody;
        private EntityToPhysicsBodyIndex m_EntityToPhysicsBody;

        internal PhysicsCallbacks Callbacks = new PhysicsCallbacks();

        private EndFramePhysicsSystem m_EndFramePhysicsSystem;

        // Schedule a callback to run at the selected phase.
        public void ScheduleCallback(PhysicsCallbacks.Phase phase, PhysicsCallbacks.Callback callback, JobHandle dependency = default(JobHandle))
        {
            Callbacks.Enqueue(phase, callback, dependency);
        }

        // Get the PhysicBodyIndex via an Entity lookup.
        public int GetPhysicsBodyIndex(Entity entity)
        {
            if (entity != Entity.Null &&
                m_EntityToPhysicsBody.Lookup.IsCreated &&
                m_EntityToPhysicsBody.Lookup.TryGetValue(entity, out int physicsBodyIndex))
                    return physicsBodyIndex;

            return PhysicsBody.Constants.InvalidBodyIndex;
        }

        // Get the PhysicBody via an Entity lookup.
        public PhysicsBody GetPhysicsBody(Entity entity)
        {
            var physicsBodyIndex = GetPhysicsBodyIndex(entity);
            if (physicsBodyIndex != PhysicsBody.Constants.InvalidBodyIndex)
            {
                var physicsBody = PhysicsWorld.AllBodies[physicsBodyIndex];
                PhysicsAssert.IsTrue(entity == physicsBody.Entity);
                return physicsBody;
            }

            return default;
        }

        protected override void OnCreate()
        {
            // Definition of a static body entity.
            StaticEntityGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PhysicsColliderBlob>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<PhysicsVelocity>()
                }
            });

            // Definition of a dynamic body entity.
            DynamicEntityGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {                    
                    ComponentType.ReadOnly<PhysicsVelocity>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<Rotation>()
                }
            });

            FinalJobHandle = default;

            PhysicsWorld = new PhysicsWorld(staticBodyCount : 0, dynamicBodyCount : 0);

            // Create the Entity to Physics Body Lookup.
            m_EntityToPhysicsBody = new EntityToPhysicsBodyIndex();
            m_EntityToPhysicsBody.Reset(0);

            m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
       }

        protected override void OnDestroy()
        {
            PhysicsWorld.Dispose();
            m_EntityToPhysicsBody.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Make sure last frame's physics jobs are complete
            m_EndFramePhysicsSystem.FinalJobHandle.Complete();

            // Update the physics world settings we have a component assigned.
            if (HasSingleton<PhysicsSettingsComponent>())
            {
                PhysicsWorld.Settings = GetSingleton<PhysicsSettingsComponent>().Value;
            }

            // Set the component system delta-time.
            PhysicsWorld.TimeStep = Time.DeltaTime;

            // Schedule phase callback.
            inputDeps = Callbacks.ScheduleCallbacksForPhase(PhysicsCallbacks.Phase.PreBuild, ref PhysicsWorld, inputDeps);

            var entityType = GetArchetypeChunkEntityType();

            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var parentType = GetArchetypeChunkComponentType<Parent>(true);
            var translationType = GetArchetypeChunkComponentType<Translation>(true);
            var rotationType = GetArchetypeChunkComponentType<Rotation>(true);
            var physicsColliderType = GetArchetypeChunkComponentType<PhysicsColliderBlob>(true);
            var physicsVelocityType = GetArchetypeChunkComponentType<PhysicsVelocity>(true);
            var physicsMassType = GetArchetypeChunkComponentType<PhysicsMass>(true);
            var physicsDampingType = GetArchetypeChunkComponentType<PhysicsDamping>(true);
            var physicsGravityType = GetArchetypeChunkComponentType<PhysicsGravity>(true);

            var staticBodyCount = StaticEntityGroup.CalculateEntityCount();
            var dynamicBodyCount = DynamicEntityGroup.CalculateEntityCount();

            var previousStaticBodyCount = PhysicsWorld.StaticBodyCount;

            // Ensure we have adequate world simulation capacity for the bodies.
            // NOTE: Add an extra static "ground" body used by joints with no entity reference.
            PhysicsWorld.Reset(
                staticBodyCount : staticBodyCount + 1,
                dynamicBodyCount : dynamicBodyCount
                );

            // Reset the Entity to Body look-up.
            // NOTE: We don't need to add the static "ground" body here.
            m_EntityToPhysicsBody.Reset(dynamicBodyCount + staticBodyCount);

            // Determine if the static bodies have changed in any way that will require the static broadphase tree to be rebuilt.
            JobHandle staticBodiesCheckHandle = default;
            var haveStaticBodiesChanged = new NativeArray<int>(1, Allocator.TempJob);
            haveStaticBodiesChanged[0] = 0;
            {
                if (PhysicsWorld.StaticBodyCount != previousStaticBodyCount)
                {
                    haveStaticBodiesChanged[0] = 1;
                }
                else
                {
                    // Make a job to test for changes
                    int numChunks;
                    using (NativeArray<ArchetypeChunk> chunks = StaticEntityGroup.CreateArchetypeChunkArray(Allocator.TempJob))
                    {
                        numChunks = chunks.Length;
                    }
                    var chunksHaveChanges = new NativeArray<int>(numChunks, Allocator.TempJob);

                    staticBodiesCheckHandle = new CheckStaticBodyChangesJob
                    {
                        LocalToWorldType = localToWorldType,
                        PhysicsColliderType = physicsColliderType,
                        ChunkHasChangesOutput = chunksHaveChanges,
                        m_LastSystemVersion = LastSystemVersion

                    }.Schedule(StaticEntityGroup, inputDeps);

                    staticBodiesCheckHandle = new CheckStaticBodyChangesReduceJob
                    {
                        ChunkHasChangesOutput = chunksHaveChanges,
                        Result = haveStaticBodiesChanged

                    }.Schedule(staticBodiesCheckHandle);
                }
            }

            using (var jobHandles = new NativeList<JobHandle>(5, Allocator.Temp))
            {
                // Static body changes check jobs
                jobHandles.Add(staticBodiesCheckHandle);

                // Create the static "ground" body used by joints with no entity reference.
                // NOTE: This will always exist as the last body in the world simulation. Could skip this if no joints present
                jobHandles.Add(new CreateStaticGroundBody
                {
                    GroundBodyIndex = PhysicsWorld.GroundBodyIndex,
                    PhysicsBodies = PhysicsWorld.AllBodies,

                }.Schedule(inputDeps));

                // Create dynamic bodies.
                if (dynamicBodyCount > 0)
                {
                    jobHandles.Add(
                        new CreatePhysicsBodiesJob
                        {
                            EntityType = entityType,
                            LocalToWorldType = localToWorldType,
                            ParentType = parentType,
                            TranslationType = translationType,
                            RotationType = rotationType,
                            ColliderType = physicsColliderType,

                            PhysicsBodies = PhysicsWorld.DynamicBodies,

                        }.Schedule(DynamicEntityGroup, inputDeps));

                    jobHandles.Add(
                        new CreatePhysicsBodyMotionsJob
                        {
                            TranslationType = translationType,
                            RotationType = rotationType,
                            ColliderType = physicsColliderType,
                            PhysicsVelocityType = physicsVelocityType,
                            PhysicsMassType = physicsMassType,
                            PhysicsDampingType = physicsDampingType,
                            PhysicsGravityType = physicsGravityType,

                            BodyMotionData = PhysicsWorld.BodyMotionData,
                            BodyMotionVelocity = PhysicsWorld.BodyMotionVelocity

                        }.Schedule(DynamicEntityGroup, inputDeps));
                }

                // Create static bodies.
                if (staticBodyCount > 0)
                {
                    jobHandles.Add(
                        new CreatePhysicsBodiesJob
                        {
                            EntityType = entityType,
                            LocalToWorldType = localToWorldType,
                            ParentType = parentType,
                            TranslationType = translationType,
                            RotationType = rotationType,
                            ColliderType = physicsColliderType,

                            PhysicsBodies = PhysicsWorld.StaticBodies,

                        }.Schedule(StaticEntityGroup, inputDeps));
                }

                // Combine all scheduled jobs.
                var handle = JobHandle.CombineDependencies(jobHandles);
                jobHandles.Clear();

                // Build the Entity to PhysicsBody Look-ups.
                var totalBodyCount = staticBodyCount + dynamicBodyCount;
                if (totalBodyCount > 0)
                {
                    handle = new CreateEntityToPhysicsBodyLookupsJob
                    {
                        PhysicsBodies = PhysicsWorld.AllBodies,
                        EntityToPhysicsBodyIndex = m_EntityToPhysicsBody.Lookup.AsParallelWriter()

                    }.Schedule(totalBodyCount, 128, handle);
                }

                // Build the broadphase.
                handle = PhysicsWorld.CollisionWorld.ScheduleBuildBroadphaseJobs(
                    ref PhysicsWorld,
                    haveStaticBodiesChanged,
                    handle);

                FinalJobHandle = haveStaticBodiesChanged.Dispose(handle);
            }

            return JobHandle.CombineDependencies(FinalJobHandle, inputDeps);
        }

        #region Jobs

        [BurstCompile]
        private struct CheckStaticBodyChangesJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsColliderBlob> PhysicsColliderType;

            public NativeArray<int> ChunkHasChangesOutput;
            public uint m_LastSystemVersion;

            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var didChunkChange =
                    chunk.DidChange(LocalToWorldType, m_LastSystemVersion) ||
                    chunk.DidChange(PhysicsColliderType, m_LastSystemVersion);

                ChunkHasChangesOutput[chunkIndex] = didChunkChange ? 1 : 0;
            }
        }

        [BurstCompile]
        private struct CheckStaticBodyChangesReduceJob : IJob
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkHasChangesOutput;
            public NativeArray<int> Result;

            public void Execute()
            {
                for (int i = 0; i < ChunkHasChangesOutput.Length; i++)
                {
                    if (ChunkHasChangesOutput[i] > 0)
                    {
                        Result[0] = 1;
                        return;
                    }
                }

                Result[0] = 0;
            }

        }

        [BurstCompile]
        private struct CreateStaticGroundBody : IJob
        {
            [ReadOnly] public int GroundBodyIndex;

            [NativeDisableContainerSafetyRestriction]
            public NativeSlice<PhysicsBody> PhysicsBodies;

            public void Execute()
            {
                PhysicsBodies[GroundBodyIndex] = PhysicsBody.Zero;
            }
        }

        [BurstCompile]
        private struct CreatePhysicsBodiesJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
            [ReadOnly] public ArchetypeChunkComponentType<Parent> ParentType;
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationType;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> RotationType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsColliderBlob> ColliderType;

            [NativeDisableContainerSafetyRestriction]
            public NativeSlice<PhysicsBody> PhysicsBodies;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(EntityType);
                var localToWorlds = chunk.GetNativeArray(LocalToWorldType);
                var translations = chunk.GetNativeArray(TranslationType);
                var rotations = chunk.GetNativeArray(RotationType);
                var colliders = chunk.GetNativeArray(ColliderType);

                var hasParentType = chunk.Has(ParentType);
                var hasTranslationType = chunk.Has(TranslationType);
                var hasRotationType = chunk.Has(RotationType);
                var hasColliderType = chunk.Has(ColliderType);

                PhysicsTransform worldTransform = PhysicsTransform.Identity;

                var instanceCount = chunk.Count;
                for(int i = 0, PhysicsBodyIndex = firstEntityIndex; i < instanceCount; ++i, ++PhysicsBodyIndex)
                {
                    if (hasParentType)
                    {
                        var localToWorld = localToWorlds[i];
                        var matrix = localToWorld.Value;
                        var orientation = quaternion.LookRotationSafe(matrix.c2.xyz, matrix.c1.xyz);
                        worldTransform = new PhysicsTransform(localToWorld.Position, orientation);
                    }
                    else
                    {
                        if (hasTranslationType)
                        {
                            worldTransform.Translation = translations[i].Value.xy;
                        }
                        else
                        {
                            worldTransform.Translation = localToWorlds[i].Position.xy;
                        }

                        if (hasRotationType)
                        {
                            worldTransform.SetQuaternionRotation(rotations[i].Value);
                        }
                        else
                        {
                            var localToWorld = localToWorlds[i];
                            var matrix = localToWorld.Value;
                            worldTransform.SetQuaternionRotation(quaternion.LookRotationSafe(matrix.c2.xyz, matrix.c1.xyz));
                        }
                    }

                    var entity = entities[i];

                    PhysicsBodies[PhysicsBodyIndex] = new PhysicsBody
                    {
                        Collider = hasColliderType ? colliders[i].Collider : default,
                        WorldTransform = worldTransform,
                        Entity = entity
                    };
                }
            }
        }

        [BurstCompile]
        private unsafe struct CreatePhysicsBodyMotionsJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<Translation> TranslationType;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> RotationType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsColliderBlob> ColliderType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsVelocity> PhysicsVelocityType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsMass> PhysicsMassType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsDamping> PhysicsDampingType;
            [ReadOnly] public ArchetypeChunkComponentType<PhysicsGravity> PhysicsGravityType;

            [NativeDisableParallelForRestriction] public NativeSlice<PhysicsBody.MotionData> BodyMotionData;
            [NativeDisableParallelForRestriction]public NativeSlice<PhysicsBody.MotionVelocity> BodyMotionVelocity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var translations = chunk.GetNativeArray(TranslationType);
                var rotations = chunk.GetNativeArray(RotationType);
                var colliders = chunk.GetNativeArray(ColliderType);
                var velocities = chunk.GetNativeArray(PhysicsVelocityType);
                var masses = chunk.GetNativeArray(PhysicsMassType);
                var dampings = chunk.GetNativeArray(PhysicsDampingType);
                var gravities = chunk.GetNativeArray(PhysicsGravityType);

                var hasColliderType = chunk.Has(ColliderType);
                var hasMassType = chunk.Has(PhysicsMassType);
                var hasDampingType = chunk.Has(PhysicsDampingType);
                var hasGravitiesType = chunk.Has(PhysicsGravityType);

                for(int i = 0, physicsBodyIndex = firstEntityIndex; i < chunk.Count; ++i, ++physicsBodyIndex)
                {                    
                    var worldTransform = new PhysicsTransform(translations[i].Value, rotations[i].Value);
                    var velocity = velocities[i];
                    var damping = hasDampingType ? dampings[i] : default;
                    var mass = hasMassType ? masses[i] : default;
                    
                    var gravityScale = 1f;
                    if (hasGravitiesType)
                    {
                        gravityScale = gravities[i].Scale;
                    }
                    else
                    {
                        // If we've got infinite mass then no gravity should be applied.
                        if (mass.InverseMass == 0f)
                            gravityScale = 0f;
                    }

                    var angularExpansionFactor = 0f;
                    if (hasColliderType)
                    {
                        var colliderBlob = colliders[i].Collider;
                        if (colliderBlob.IsCreated)
                            angularExpansionFactor = colliderBlob.Value.MassProperties.AngularExpansionFactor;
                    }

                    BodyMotionData[physicsBodyIndex] = new PhysicsBody.MotionData
                    {                       
                        WorldPosition = worldTransform.Translation,
                        WorldAngle = PhysicsMath.angle(worldTransform.Rotation),

                        LocalCenterOfMass = mass.LocalCenterOfMass,
                        GravityScale = gravityScale,

                        LinearDamping = damping.Linear,
                        AngularDamping = damping.Angular
                    };

                    BodyMotionVelocity[physicsBodyIndex] = new PhysicsBody.MotionVelocity
                    {
                        LinearVelocity = velocity.Linear,
                        AngularVelocity = velocity.Angular,

                        InverseMass = mass.InverseMass,
                        InverseInertia = mass.InverseInertia,

                        AngularExpansionFactor = angularExpansionFactor
                    };
                }
            }
        }

        [BurstCompile]
        private struct CreateEntityToPhysicsBodyLookupsJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<PhysicsBody> PhysicsBodies;
            public NativeHashMap<Entity, int>.ParallelWriter EntityToPhysicsBodyIndex;

            public void Execute(int index)
            {
                // Add the Entity to the lookup.
                EntityToPhysicsBodyIndex.TryAdd(PhysicsBodies[index].Entity, index);
            }
        }

        #endregion
    }
}
