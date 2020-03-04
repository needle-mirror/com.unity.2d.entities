# 2D Physics example

## Introduction

The following is an example of code for a 2D Physics system in the context of a game. It is taken from the __TinySpaceship__ demo Project which is available from the [Project Tiny Samples ](https://github.com/Unity-Technologies/ProjectTinySamples)GitHub repo (filepath: ``[Root]/TinySpaceship/Assets/TinySpaceship/Scripts/Runtime/MissileHitSystem.cs``). The example demonstrates how to perform a Collider in Collider check. 

## The complete code example

```
using Unity.Entities;
using Unity.Transforms;
using Unity.U2D.Entities.Physics;

public class MissileHitSystem : ComponentSystem
{  
    protected override void OnUpdate()
    {
        var physicsWorldSystem = World.GetExistingSystem<PhysicsWorldSystem>();
        var physicsWorld = physicsWorldSystem.PhysicsWorld;

        var didExplode = false;

        Entities.WithAll<Missile>()
            .ForEach((
                Entity missileEntity, 
                ref PhysicsColliderBlob collider, 
                ref Translation tr, 
                ref Rotation rot) =>
            {
                if (physicsWorld.OverlapCollider(
                    new OverlapColliderInput
                    {
                        Collider = collider.Collider,
                        Transform = new PhysicsTransform(tr.Value, rot.Value),
                        Filter = collider.Collider.Value.Filter
                    },
                    out OverlapColliderHit hit))
                {
                    var asteroidEntity = physicsWorld.AllBodies[hit.PhysicsBodyIndex].Entity;

                    PostUpdateCommands.DestroyEntity(asteroidEntity);
                    PostUpdateCommands.DestroyEntity(missileEntity);

                    didExplode = true;
                }
            });

        if (didExplode)
        {
            var explosionSfx = AudioTypes.AsteroidExplosionLarge;
            AudioUtils.PlaySound(EntityManager, explosionSfx);   
        }
    }
}
```

## The code step-by-step

```
public class MissileHitSystem : ComponentSystem
```

This system is designed to iterate all missiles, detect if they hit an asteroid and if so, play a sound.

### PhysicsWorldSystem

This is the main system that's responsible for all 2D physics and is one of the most commonly used. It is the first of the physics systems to run. 

```
var physicsWorldSystem = World.GetExistingSystem<PhysicsWorldSystem>();
```

With a reference to this system you'll be able to access all of the important physics structures to perform queries, schedule job callbacks at important physics pipeline stages, read data such as `PhysicsBody` information and so on. 

Although not shown in this example, like most systems, you can grab this system once at the start for reference and refer to it as needed.

### PhysicsWorld

`PhysicsWorld` is a common structure that is built at each simulation step. It encapsulates all `PhysicsBody` and both Static and Dynamic Colliders. It also contains information such as motion data for Dynamic PhysicsBody that is used in advanced cases. The main reason to use the `PhysicsWorld` is to perform queries.

```
var physicsWorld = physicsWorldSystem.PhysicsWorld;
```

Like most of the `PhysicSystem` data, `PhysicsWorld` is contained in a struct (value type) and is treated as a struct. This means you can pass them around by value without a problem and they'll continue to refer to the same logical `PhysicsWorld`. The `PhysicsWorld` has multiple methods, including the 'Clone' method that allows you to make a completely separate copy of the `PhysicsWorld`. This clone contains everything that belonged to the original, and can be used in isolation for any purpose such as queries, networking, simulation and so on.

All the available queries are defined by an interface of `IQueryable` which shows every type of query that you can perform. `PhysicsWorld` can perform queries such as `OverlapPoint` and  `CastRay`. Although `PhysicsWorld`  is the most commonly used, other components such as `PhysicsBody` and Colliders can perform queries as well. In this example, the query is for the world.

### Entities.ForEach

The following code iterates all Entities that have the empty `Missile` component. The `Missile` component does not contain data but acts as a 'tag' component that identifies the Entity. In this example, the code filters the Entities for Entities with the `missiles` tag.

```
Entities.WithAll<Missile>()
    .ForEach((
      Entity missileEntity, 
      ref PhysicsColliderBlob collider, 
      ref Translation tr, 
      ref Rotation rot) =>
             {
```

Use the current Translation and Rotation of the missile Entity to specify where the Collider is in the query.

### OverlapCollider

```c#
if (physicsWorld.OverlapCollider(
```

The query above translates to "Does this `Collider` at this position and rotation using this collision-filter, overlap anything?"

In the case of the missiles, this code performs a simple query that takes the Collider attached to a 'missile' and checks if it collides with anything in the physics world. It only checks the first collision of the 'missile' and whether it is between it and an 'asteroid', as the goal is to destroy both the missile and asteroid on collision. Although not shown here, you can perform more advanced queries which check if the missile would hit anything in its current direction for the current delta-time so that asteroids won't be missed.

A query is roughly composed of three important parts:
1. The query itself. In this case is a query on the whole `PhysicsWorld` checking if a `Collider` overlaps.
2. The query input. Every query type (for example `OverlapCollider` ,`OverlapPoint` and so on) has its own dedicated query input struct which you populate.
3. The query output. In the `IQueryable` interface, you'll see that all queries have several identical overloads that let you:
    - Only return true/false if the query detected anything. For example: `bool CastRay(RaycastInput input);`
    - Return true/false if the query detected anything but also return the first hit. For example: `bool OverlapCollider(OverlapColliderInput input, out OverlapColliderHit hit)` (you provide a Hit structure directly).
    - Return true/false if the query detected anything but also return all hits. For example: `bool OverlapCollider(OverlapColliderInput input, ref NativeList<OverlapColliderHit> allHits)`
    - Return true/false if the query detected anything but also lets you provide an arbitrary collector for the hits. For example: `bool OverlapCollider<T>(OverlapColliderInput input, ref T collector) where T : struct, ICollector<OverlapColliderHit>`

Note that internally, all queries use 'collectors'. A collector is defined by the interface `ICollector<Hit>` which you can look-up and is a mechanism to store query results also known as "hits". Unity provides three collectors which you can use: `AnyHitCollector<Hit>`, `ClosestHitCollector<Hit>` and `AllHitsCollector<Hit>` but you are free to create your own. Note that each query has its own dedicated output struct as well.

In the example below showcasing the`OverlapCollider`, only the closest hit is considered (it actually uses the `ClosestHitCollector` internally for you).

### OverlapColliderInput

This is the query input structure:

```
new OverlapColliderInput
{
```

You don't have to populate it here but could instead do it earlier and reuse it if appropriate.

#### Collider

This is the `Collider` which is used against the `PhysicsWorld` to see if it overlaps with any other `Collider`.

```c#
Collider = collider.Collider,
```

#### Transform

This is the `PhysicsTransform` of the above `Collider`.

```c#
 Transform = new PhysicsTransform(tr.Value, rot.Value),
```

You can specify any `Collider` at any position and/or rotation. You can produce a `PhysicsTransform` here which is a dedicated 2D transform used throughout physics. It removes some of the 3D transformations that are not suitable for 2D physics. When you want to create a PhysicsTransform, there are multiple constructors and helper methods to create a PhysicsTransform. It accepts many 3D arguments such as `float3` and `quaternion` (from the `Transform` system) as well as standard 2D ones such as `float2` (translation) and `float2x2` (rotation).

#### Filter

This refers to the `CollisionFilter` used. The `CollisionFilter` struct allows you to control a set of 32 layers that a `Collider` belongs to as well as a set of 32 layers that it can collide with.

```
	Filter = collider.Collider.Value.Filter
},
```

Here you can specify anything you want such as `CollisionFilter.Default` which will collide with everything. This example uses the `CollisionFilter` assigned to the missile `Collider`. As this example comes from the TinySpaceship demo Project, the `Colliders` come from the Classic Unity 2D physics system via the `GameObject` Conversion system. That system automatically transfers the layer these `Colliders` are on to the DOTS Collider, and also sets up which layers can contact which layers which is defined in the Layer Collision Matrix. Refer to the [Physics 2D](https://docs.unity3d.com/Manual/class-Physics2DManager.html) documentation for more information.

### OverlapColliderHit

This is the expected output. It is a dedicated struct for this query and contains information relevant to this query type only.

```c#
out OverlapColliderHit hit))
{
```

Note that it will be set at default (empty) if the query returns false as that indicates there was no hit.

### PhysicsBody

This part of the code only runs if the query returns true, indicating that a missile `Collider` overlaps something. As the Layer Collision Matrix is setup to only hit things specified, the other object can be assumed to be an asteroid as they are on a specific layer. The goal is to find the ECS Entity of what was hit. 

```c#
var asteroidEntity = physicsWorld.AllBodies[hit.PhysicsBodyIndex].Entity;
```

All queries (and physics) however do not work with ECS components but actually have their own representation that is built, per simulation step, from the ECS components. This is done for performance reasons, as physics that perform non-sequential (random) read/write operations would perform poorly in an ECS system.

All queries will return the `PhysicsBody` index that was intersected as well as other relevant hit information. A `PhysicsBody` is similar to the classic `Rigidbody2D` but is a more efficient representation, as returning the `PhysicsBody` index as an "int" is a more efficient way to refer to the data. This is especially true if your query were to return thousands of hits. Rather than thousands of copies of `PhysicsBody`, you'd have thousands of "int" (indexes) which use less memory and fewer cache-misses.

The `PhysicsWorld` provides a flat array of all bodies as seen here in `AllBodies`. Passing in the `PhysicsBody` index returns a `PhysicsBody`.  You get the `PhysicsBody` from the array but then immediately access its `Entity` property which is the `Entity` that was used to build the `PhysicsBody`.
Note that you do not need to check the index against this array. If it's returned, it'll be within the valid range of all the bodies. You get the `Entity` of what was hit and because of the `CollisionFilter`, it is implicitly known that it's an asteroid.

### DestroyEntity

Both the asteroid and missile Entities can now be destroyed. You can create a more advanced version of the code to break up the asteroid into smaller parts by instantiating several new and smaller ones.

```
PostUpdateCommands.DestroyEntity(asteroidEntity);
PostUpdateCommands.DestroyEntity(missileEntity);
```