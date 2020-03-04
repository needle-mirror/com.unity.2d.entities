# Collision Queries

Unity Physics 2D follows (almost identically) the feature set provided by Unity Physics (3D) and nearly all of the [documentation](https://docs.unity3d.com/Packages/com.unity.physics@0.2/manual/collision_queries.html) for that package is identical however 2D also provides additional Overlap queries. Nevertheless, below is an overview which should highlight differences between the two packages.

Queries can be executed safely on jobs and on any type that implements the IQueryable interface. This means queries can be local (bodies and colliders) or global (entire PhysicsWorld). Types that implement this are:

* PhysicsBody

* All Colliders (PhysicsCircleCollider, PhysicsCapsuleCollider, PhysicsBoxCollider & PhysicsPolygonCollider).

* PhysicsWorld (via CollisionWorld and Broadphase)

The IQueryable interface consistently exposes the following queries:

| __Query Type__                   | __Inputs__                                                   | __Description__                                              |
| -------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| __CastRay__                      | RaycastInputStartEndFilter (CollisionFilter)Ignore (Ignore PhysicsBody) | Casts a ray between the Start and End positions, filtering hits by Filter and ignoring any (optionally) specified PhysicsBody. |
| __CastCollider__                 | ColliderCastInputStartEndRotationColliderIgnore (Ignore PhysicsBody) | Casts the Collider between the Start and End positions at the specified Rotation, filtering hits by the Collider Filter and ignoring any (optionally) specified PhysicsBody. |
| __OverlapPoint__                 | OverlapPointInputPositionFilter (CollisionFilter)Ignore (Ignore PhysicsBody) | Determines if the point at Position overlaps, filtering hits by Filter and ignoring any (optionally) specified PhysicsBody. |
| __OverlapCollider__              | OverlapColliderInputColliderTransform (PhysicsTransform)ColliderIgnore (Ignore PhysicsBody) | Determines if the Collider at the specified Transform, overlaps, filtering hits by the Collider Filter and ignoring any (optionally) specified PhysicsBody. |
| __CalculateDistance (Point)__    | PointDistanceInputPositionMaxDistanceFilter (CollisionFilter)Ignore (Ignore PhysicsBody) | Calculates the distance of the point at Position, limited by a maximum distance of MaxDistance, filtering hits by Filter and ignoring any (optionally) specified PhysicsBody. |
| __CalculateDistance (Collider)__ | ColliderDistanceInputColliderTransform (PhysicsTransform)MaxDistanceIgnore (Ignore PhysicsBody) | Calculates the distance of the Collider at the specified Transform, limited by a maximum distance of MaxDistance, filtering hits by the Collider Filter and ignoring any (optionally) specified PhysicsBody. |

Each of the above queries come in four overloads. All return a bool indicating whether any hits were found or not.

The four overloads are:

- No hits results returned (use return value only to indicate a hit)
  - Example: `bool CastRay(RaycastInput input);`
- Closest hit returned only
- Example: `bool CastRay(RaycastInput input, out RaycastHit closestHit);`
- All hits returned
- Example: `bool CastRay(RaycastInput input, ref NativeList<RaycastHit> allHits);`
- Custom hit collector
- Example: `bool CastRay<T>(RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>;`

The `ICollector<>` interface provides the ability to collect hits using your own types, allowing you to implement collection behavior.  Three existing behaviors are provided which are already used in the implemented queries above.  These are:

* `AllHitsCollector<T>` : Collects all hits.
* `AnyHitCollector<T>` : Collects the first hit
* `ClosestHitCollector<T>` : Collects the closest hit



## Collision Filtering

Collision filtering is specified with the CollisionFilter type and is identical to the one used in the Unity Physics (3D) package. See the section on [Filtering](https://docs.unity3d.com/Packages/com.unity.physics@0.2/manual/collision_queries.html#Collectors) for more information.

Collision filters are set on Colliders but, as can be seen above, can also be explicitly specified in several queries allowing filtering of results (hits).

A default CollisionFilter that effectively allows all hits can be accessed via the static property of "CollisionFilter.Default”.

## Query Outputs

Many of the queries produce similar results (hits). These hits have a subset of the following fields:

| __Output Field__     | __Description__                                              |
| -------------------- | ------------------------------------------------------------ |
| __Fraction__         | The proportion along a line segment defined by a Start and End position (CastRay or CastCollider) as the point of intersection (0 to 1). Note that for Overlap queries, the Fraction value is always equal to zero. |
| __Position__         | The point of intersection in world space.                    |
| __SurfaceNormal__    | The normal to the surface at the point of intersection in world space. |
| __PhysicsBodyIndex__ | The index of the PhysicsBody (CollisionWorld) in the PhysicsWorld query was performed. |
| __Entity__           | The Entity of the Collider that the query found. This is the Collider attached to the PhysicsBody referred to by PhysicsBodyIndex. |
| __ColliderKey__      | Internal information about which part of a composite shape was hit. This is not currently supported until CompoundCollider support is released. |

These are the outputs of the respective queries:

| __Query Type__                                               | __Outputs__                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| __CastRay__                                                  | RaycastHit<br />- Fraction<br />- Position<br />- SurfaceNormal<br />- PhysicsBodyIndex<br />- Entity<br />- ColliderKey |
| __CastCollider__                                             | ColliderCastHit<br />- Fraction<br />- Position<br />- SurfaceNormal<br />- PhysicsBodyIndex<br />- Entity<br />- ColliderKey |
| __OverlapPoint__                                             | OverlapPointHit<br />- Fraction (Always zero!)<br />- Position (Always Input Position)<br />- PhysicsBodyIndex<br />- Entity<br />- ColliderKey |
| __OverlapCollider__                                          | OverlapColliderHit<br />- Fraction (Always zero!)<br />- PhysicsBodyIndex<br />- Entity<br />- ColliderKey |
| __CalculateDistance (Point)__ and<br/> __CalculateDistance (Collider)__ | DistanceHit<br />- Fraction (as this value is unused, it is set to the same value as ``Distance``)<br />- Distance : Calculated Distance<br />- Direction : Direction of Hit (Always Point B - PointA)<br />- PointA : Position (on Point or Source Collider)<br />- PointB : Position on Hit Collider<br />- PhysicsBodyIndex<br />- Entity<br />- ColliderKey |

---

If you have any issues or questions about the 2D Entities package and its features, please visit the [Project Tiny](https://forum.unity.com/forums/project-tiny.151/) forum and [First batch of 2D Features for Project Tiny is now available](https://forum.unity.com/threads/first-batch-of-2d-features-for-project-tiny-is-now-available.830652/) thread for more information and discussions with the development team.