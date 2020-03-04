using System;

using Unity.Entities;
using Unity.Mathematics;
using static Unity.U2D.Entities.Physics.PhysicsMath;

namespace Unity.U2D.Entities.Physics
{
    #region Query Input & Output

    public struct OverlapAabbInput : IQueryInput
    {
        public Aabb Aabb;
        public CollisionFilter Filter;
        public IgnoreHit Ignore { get; set; }
    }

    public struct OverlapPointInput : IQueryInput
    {
        public float2 Position;
        public CollisionFilter Filter;
        public IgnoreHit Ignore { get; set; }

        internal QueryContext QueryContext;
    }

    public struct OverlapPointHit : IQueryResult
    {
        #region IQueryResult

        public int PhysicsBodyIndex { get; internal set; }
        public ColliderKey ColliderKey { get; internal set; }
        public Entity Entity { get; internal set; }
        public float Fraction { get; internal set; }

        public bool IsValid => Entity != Entity.Null;

        #endregion

        public float2 Position { get; internal set; }
    }

    public struct OverlapColliderInput : IQueryInput
    {
        public BlobAssetReference<Collider> Collider;
        public PhysicsTransform Transform;
        public CollisionFilter Filter;
        public IgnoreHit Ignore { get; set; }

        internal QueryContext QueryContext;
    }

    public struct OverlapColliderHit : IQueryResult
    {
        #region IQueryResult

        public int PhysicsBodyIndex { get; internal set; }
        public ColliderKey ColliderKey { get; internal set; }
        public Entity Entity { get; internal set; }
        public float Fraction { get; internal set; }
        
        public bool IsValid => Entity != Entity.Null;
        
        #endregion
    }

    // Interface for collecting hits from overlap queries
    public interface IOverlapCollector
    {
        unsafe void AddPhysicsBodyIndices(int* indices, int count);
        unsafe void AddColliderKeys(ColliderKey* keys, int count);
        void PushCompositeCollider(ColliderKeyPath compositeKey);
        void PopCompositeCollider(uint numCompositeKeyBits);
    }

    #endregion

    // Overlap query implementations
    public static class OverlapQueries
    {
        #region Overlap Point

        // Point/Convex intersection.
        private static unsafe bool PointConvex(float2 point, ref ConvexHull hull)
        {
            float2* vertices = hull.Vertices.GetUnsafePtr();
            float2* normals = hull.Normals.GetUnsafePtr();

            var length = hull.Length;
            for(var i = 0; i < length; ++i)
            {
                if (math.dot(*normals++, point - *vertices++) > 0f)
                    return false;
            }

            return true;
        }

        // Point/Capsule intersection.
        internal static bool PointCapsule(float2 point, float2 vertex0, float2 vertex1, float radius)
        {
            var localDistance = QueryUtility.NearestPointOnLineSegment(point, vertex0, vertex1) - point;
            return math.lengthsq(localDistance) <= (radius * radius);
        }

        // Point/Circle intersection.
        private static bool PointCircle(float2 point, float2 center, float radius)
        {
            return math.lengthsq(point - center) <= (radius * radius);
        }

        internal static unsafe bool OverlapPoint<T>(OverlapPointInput input, Collider* collider, ref T collector) where T : struct, ICollector<OverlapPointHit>
        {
            // Nothing to do if:
            // - MaxFraction is zero.
            // - Filtered out.
            if (collector.MaxFraction == 0f ||
                !CollisionFilter.IsCollisionEnabled(input.Filter, collider->Filter))
            {
                return false;
            }

            // Ensure the query context is initialized.
            input.QueryContext.EnsureIsInitialized();

            bool hadHit;
            switch (collider->ColliderType)
            {
                case ColliderType.Box:
                    {
                        var box = (PhysicsBoxCollider*)collider;
                        hadHit = PointConvex(input.Position, ref box->m_ConvexHull);
                        break;
                    }

                case ColliderType.Polygon:
                    {
                        var polygon = (PhysicsPolygonCollider*)collider;
                        hadHit = PointConvex(input.Position, ref polygon->m_ConvexHull);
                        break;
                    }

                case ColliderType.Capsule:
                    {
                        var capsule = (PhysicsCapsuleCollider*)collider;
                        hadHit = PointCapsule(input.Position, capsule->Vertex0, capsule->Vertex1, capsule->Radius);
                        break;
                    }

                case ColliderType.Circle:
                    {
                        var circle = (PhysicsCircleCollider*)collider;
                        hadHit = PointCircle(input.Position, circle->Center, circle->Radius);
                        break;
                    }

                case ColliderType.Compound:
                    {
                        return PointCompound(input, (PhysicsCompoundCollider*)collider, ref collector);
                    }

                default:
                    throw new NotImplementedException();
            }

            if (hadHit)
            {
                var hit = new OverlapPointHit
                {
                    Fraction = 0f,
                    Position = mul(input.QueryContext.LocalToWorldTransform, input.Position),

                    PhysicsBodyIndex = input.QueryContext.PhysicsBodyIndex,
                    ColliderKey = input.QueryContext.ColliderKey,
                    Entity = input.QueryContext.Entity
                };                

                return collector.AddHit(hit);
            }
            return false;
        }

        #region Overlap Point Compound

        private unsafe struct OverlapPointCompoundLeafProcessor : BoundingVolumeHierarchy.IPointOverlapLeafProcessor
        {
            private readonly PhysicsCompoundCollider* m_CompoundCollider;

            public OverlapPointCompoundLeafProcessor(PhysicsCompoundCollider* compoundCollider)
            {
                m_CompoundCollider = compoundCollider;
            }

            public bool PointLeaf<T>(OverlapPointInput input, int leafData, ref T collector) where T : struct, ICollector<OverlapPointHit>
            {
                ref PhysicsCompoundCollider.Child child = ref m_CompoundCollider->Children[leafData];

                if (!CollisionFilter.IsCollisionEnabled(input.Filter, child.Collider->Filter))
                {
                    return false;
                }

                // Transform the point into child space.
                OverlapPointInput inputLs = input;
                {
                    var compoundFromChild = child.CompoundFromChild;
                    var childFromCompound = inverse(compoundFromChild);

                    inputLs.Position = mul(childFromCompound, input.Position);

                    inputLs.QueryContext.ColliderKey = input.QueryContext.PushSubKey(m_CompoundCollider->NumColliderKeyBits, (uint)leafData);
                    inputLs.QueryContext.NumColliderKeyBits = input.QueryContext.NumColliderKeyBits;
                    inputLs.QueryContext.LocalToWorldTransform = mul(input.QueryContext.LocalToWorldTransform, compoundFromChild);
                }

                return child.Collider->OverlapPoint(inputLs, ref collector);
            }
        }

        private static unsafe bool PointCompound<T>(OverlapPointInput input, PhysicsCompoundCollider* compoundCollider, ref T collector) where T : struct, ICollector<OverlapPointHit>
        {
            if (!CollisionFilter.IsCollisionEnabled(input.Filter, compoundCollider->Filter))
            {
                return false;
            }

            var leafProcessor = new OverlapPointCompoundLeafProcessor(compoundCollider);
            return compoundCollider->BoundingVolumeHierarchy.OverlapPoint(input, ref leafProcessor, ref collector);
        }

        #endregion

        #endregion

        #region Overlap Collider

        internal static bool OverlapConvexConvex(ref PhysicsTransform transform, ref DistanceProxy proxySource, ref DistanceProxy proxyTarget)
        {
            var hit = DistanceQueries.ColliderDistance(transform, ref proxySource, ref proxyTarget);

            return hit.Distance < 10f * float.Epsilon;
        }

        private static unsafe bool OverlapConvex(ref OverlapColliderInput input, ref DistanceProxy proxyTarget)
        {
            if (!input.Collider.IsCreated)
                return false;

            var inputColliderBlob = input.Collider;
            switch (inputColliderBlob.Value.ColliderType)
            {
                case ColliderType.Box:
                case ColliderType.Polygon:
                case ColliderType.Capsule:
                case ColliderType.Circle:
                    {
                        ref var convexHull = ref inputColliderBlob.GetColliderRef<ConvexCollider>().m_ConvexHull;
                        var proxySource = new DistanceProxy(ref convexHull);
                        return OverlapConvexConvex(ref input.Transform, ref proxySource, ref proxyTarget);
                    }

                case ColliderType.Compound:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        internal static unsafe bool OverlapCollider<T>(OverlapColliderInput input, Collider* collider, ref T collector) where T : struct, ICollector<OverlapColliderHit>
        {
            // Nothing to do if:
            // - MaxFraction is zero.
            // - The Collider isn't valid.
            // - Filtered out.
            if (collector.MaxFraction < float.Epsilon ||
                !input.Collider.IsCreated ||
                !CollisionFilter.IsCollisionEnabled(input.Filter, collider->Filter))
            {
                return false;
            }

            // Ensure the query context is initialized.
            input.QueryContext.EnsureIsInitialized();

            bool hadHit;
            var inputColliderBlob = input.Collider;
            switch (inputColliderBlob.Value.CollisionType)
            {
                case CollisionType.Convex:
                    {
                        switch (collider->ColliderType)
                        {
                            case ColliderType.Box:
                            case ColliderType.Polygon:
                            case ColliderType.Capsule:
                            case ColliderType.Circle:
                                {
                                    var convexCollider = (ConvexCollider*)collider;
                                    var proxyTarget = new DistanceProxy(ref convexCollider->m_ConvexHull);
                                    hadHit = OverlapConvex(ref input, ref proxyTarget);
                                    break;
                                }

                            case ColliderType.Compound:
                                {
                                    return OverlapCompound(input, (PhysicsCompoundCollider*)collider, ref collector);
                                }

                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    }

                case CollisionType.Composite:
                    {
                        // Compound overlaps not supported.
                        throw new NotImplementedException();
                    }

                default:
                    throw new NotImplementedException();
            }

            if (hadHit)
            {
                var hit = new OverlapColliderHit
                {
                    Fraction = 0f,

                    PhysicsBodyIndex = input.QueryContext.PhysicsBodyIndex,
                    ColliderKey = input.QueryContext.ColliderKey,
                    Entity = input.QueryContext.Entity
                };

                return collector.AddHit(hit);
            }
            return false;
        }

        #region Overlap Collider Compound

        private unsafe struct OverlapCompoundLeafProcessor : BoundingVolumeHierarchy.IColliderOverlapLeafProcessor
        {
            readonly PhysicsCompoundCollider* m_CompoundCollider;

            public OverlapCompoundLeafProcessor(PhysicsCompoundCollider* compoundCollider)
            {
                m_CompoundCollider = compoundCollider;
            }

            public bool ColliderLeaf<T>(OverlapColliderInput input, int leafData, ref T collector) where T : struct, ICollector<OverlapColliderHit>
            {
                ref PhysicsCompoundCollider.Child child = ref m_CompoundCollider->Children[leafData];

                if (!CollisionFilter.IsCollisionEnabled(input.Filter, child.Collider->Filter))
                {
                    return false;
                }

                // Transform the query into child space
                OverlapColliderInput inputLs = input;
                {
                    var compoundFromChild = child.CompoundFromChild;
                    var childFromCompound = inverse(child.CompoundFromChild);

                    inputLs.Transform = mul(childFromCompound, input.Transform);

                    inputLs.QueryContext.ColliderKey = input.QueryContext.PushSubKey(m_CompoundCollider->NumColliderKeyBits, (uint)leafData);
                    inputLs.QueryContext.NumColliderKeyBits = input.QueryContext.NumColliderKeyBits;
                    inputLs.QueryContext.LocalToWorldTransform = mul(input.QueryContext.LocalToWorldTransform, compoundFromChild);
                }

                return child.Collider->OverlapCollider(inputLs, ref collector);
            }
        }

        private static unsafe bool OverlapCompound<T>(OverlapColliderInput input, PhysicsCompoundCollider* compoundCollider, ref T collector) where T : struct, ICollector<OverlapColliderHit>
        {
            if (!CollisionFilter.IsCollisionEnabled(input.Filter, compoundCollider->Filter))
            {
                return false;
            }

            var leafProcessor = new OverlapCompoundLeafProcessor(compoundCollider);
            return compoundCollider->BoundingVolumeHierarchy.OverlapCollider(input, ref leafProcessor, ref collector);
        }

        #endregion

        #endregion

        #region Overlap AABB

        internal static unsafe void AabbCollider<T>(OverlapAabbInput input, Collider* collider, ref T collector)
            where T : struct, IOverlapCollector
        {
            if (!CollisionFilter.IsCollisionEnabled(input.Filter, collider->Filter))
            {
                return;
            }

            switch (collider->ColliderType)
            {
                case ColliderType.Compound:
                    AabbCompound(input, (PhysicsCompoundCollider*)collider, ref collector);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private unsafe struct AabbOverlapCompoundLeafProcessor : BoundingVolumeHierarchy.IAabbOverlapLeafProcessor
        {
            readonly PhysicsCompoundCollider* m_CompoundCollider;
            readonly uint m_NumColliderKeyBits;

            const int k_MaxKeys = 512;
            fixed uint m_Keys[k_MaxKeys];  // actually ColliderKeys, but C# doesn't allow fixed arrays of structs
            int m_NumKeys;

            public AabbOverlapCompoundLeafProcessor(PhysicsCompoundCollider* compoundCollider)
            {
                m_CompoundCollider = compoundCollider;
                m_NumColliderKeyBits = compoundCollider->NumColliderKeyBits;
                m_NumKeys = 0;
            }

            public void AabbLeaf<T>(OverlapAabbInput input, int leafData, ref T collector) where T : struct, IOverlapCollector
            {
                ColliderKey childKey = new ColliderKey(m_NumColliderKeyBits, (uint)(leafData));

                // Recurse if child is a composite
                ref PhysicsCompoundCollider.Child child = ref m_CompoundCollider->Children[leafData];
                if (child.Collider->CollisionType == CollisionType.Composite)
                {
                    OverlapAabbInput childInput = input;
                    childInput.Aabb = mul(inverse(child.CompoundFromChild), input.Aabb);

                    collector.PushCompositeCollider(new ColliderKeyPath(childKey, m_NumColliderKeyBits));
                    AabbCollider(childInput, child.Collider, ref collector);
                    collector.PopCompositeCollider(m_NumColliderKeyBits);
                }
                else
                {
                    m_Keys[m_NumKeys++] = childKey.Value;
                    if (m_NumKeys > k_MaxKeys - 8)
                    {
                        Flush(ref collector);
                    }
                }
            }

            // Flush keys to collector
            internal void Flush<T>(ref T collector) where T : struct, IOverlapCollector
            {
                fixed (uint* keys = m_Keys)
                {
                    collector.AddColliderKeys((ColliderKey*)keys, m_NumKeys);
                }
                m_NumKeys = 0;
            }
        }

        private static unsafe void AabbCompound<T>(OverlapAabbInput input, PhysicsCompoundCollider* compound, ref T collector)
            where T : struct, IOverlapCollector
        {
            var leafProcessor = new AabbOverlapCompoundLeafProcessor(compound);
            compound->BoundingVolumeHierarchy.AabbOverlap(input, ref leafProcessor, ref collector);
            leafProcessor.Flush(ref collector);
        }

        #endregion
    }
}
