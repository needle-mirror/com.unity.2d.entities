# GameObject to ECS conversion

For this release, only basic GameObject conversion is currently supported for the [Rigidbody2D ](https://docs.unity3d.com/ScriptReference/Rigidbody2D.html)and certain [Collider2D](https://docs.unity3d.com/ScriptReference/Collider2D.html). There are no conversions for [Joint2D](https://docs.unity3d.com/ScriptReference/Joint2D.html). In a future release, we will add dedicated authoring components that do not require conversion from native 2D Physics.

## Rigidbody2D

A Rigidbody2D will be automatically converted into the following ECS components:

| ECS Component   | What it contains                                             |
| --------------- | ------------------------------------------------------------ |
| PhysicsVelocity | Linear and Angular velocities.                               |
| PhysicsDamping  | Linear and Angular damping of velocities. These will be taken from the Rigidbody2D.drag and Rigidbody2D.angularDrag properties respectively. |
| PhysicsMass     | Mass (inverse), Rotational Inertia (inverse) and (local) Center of Mass.<br/>These will be taken from the Rigidbody2D.mass, Rigidbody2D.inertia and Rigidbody2D.centerOfMass properties respectively. |
| PhysicsGravity  | Scaling factor for world gravity. This will be taken from the Rigidbody2D.gravityScale property. |

A __Dynamic__ Rigidbody2D (with [bodyType](https://docs.unity3d.com/ScriptReference/Rigidbody2D-bodyType.html) set to Dynamic) will generate all the above components.

A __Kinematic__ Rigidbody2D (with [bodyType](https://docs.unity3d.com/ScriptReference/Rigidbody2D-bodyType.html) set to Kinematic) will  generate only the PhysicsVelocity component above.

A __Static__ Rigidbody2D (a GameObject without a Rigidbody2D or one with [bodyType](https://docs.unity3d.com/ScriptReference/Rigidbody2D-bodyType.html) set to Static) will not generate any of the above components. A Static body will only contain collider components (see below).

| Rigidbody2D [bodyType](https://docs.unity3d.com/ScriptReference/Rigidbody2D-bodyType.html) | Generated ECS components                      |
| ------------------------------------------------------------ | --------------------------------------------- |
| __Dynamic__                                                  | PhysicsVelocity, PhysicsMass, PhysicsGravity. |
| __Kinematic__                                                | PhysicsVelocity only.                         |
| __Static; or a GameObject without a RigidBody2D__            | None                                          |

A __Static__ Rigidbody2D (a GameObject without a Rigidbody2D or one with [bodyType](https://docs.unity3d.com/ScriptReference/Rigidbody2D-bodyType.html) set to Static) will not generate any of the above components. A Static body will only contain [Collider](https://docs.google.com/document/d/1Wpa1mWKz2-hnjFnT_Bcx_pR0tnPwBGxiYVrVUjFLExA/edit#heading=h.5caum480oow3) components.

Notes:

- If the [Rigidbody2D.Simulated](https://docs.unity3d.com/ScriptReference/Rigidbody2D-simulated.html) property is false then the body will not be converted.
- When going into Play mode, the classic 2D physics system will run so class GameObject components in a sub-scene will be simulated when the sub-scene is open for editing. This won’t be the case when the sub-scene is closed or for player builds. To get around this, you can turn off 2D auto-simulation in the Project Settings > Physics 2D > Auto Simulation or via [script](https://docs.unity3d.com/ScriptReference/Physics2D-autoSimulation.html).



## Collider2D

The following components and specified properties are converted to a DOTS collider known as a BlobAssetReference<Collider> (see below). If the collider isn’t [Enabled](https://docs.unity3d.com/ScriptReference/Behaviour-enabled.html) or its [usedByComposite](https://docs.unity3d.com/ScriptReference/Collider2D-usedByComposite.html) property is true then it will not be converted.

__Note:__ Multiple Collider 2D on the same GameObject will produce a warning from the GameObject conversion system itself as it does not support multiple components on the same Entity. This is a known limitation but the restriction will be removed later.

| Collider 2D           | Collider 2D Properties                                       |
| --------------------- | ------------------------------------------------------------ |
| __BoxCollider2D__     | [Collider2D.offset](https://docs.unity3d.com/ScriptReference/Collider2D-offset.html)<br />[BoxCollider2D.size](https://docs.unity3d.com/ScriptReference/BoxCollider2D-size.html)<br />[BoxCollider2D.edgeRadius](https://docs.unity3d.com/ScriptReference/BoxCollider2D-edgeRadius.html) |
| __CapsuleCollider2D__ | [Collider2D.offset](https://docs.unity3d.com/ScriptReference/Collider2D-offset.html)<br />[CapsuleCollider2D.direction](https://docs.unity3d.com/ScriptReference/CapsuleCollider2D-direction.html)<br />[CapsuleCollider2D.size](https://docs.unity3d.com/ScriptReference/CapsuleCollider2D-size.html) |
| __CircleCollider2D__  | [Collider2D.offset](https://docs.unity3d.com/ScriptReference/Collider2D-offset.html)<br />[CircleCollider2D.radius](https://docs.unity3d.com/ScriptReference/CircleCollider2D-radius.html) |
| __PolygonCollider2D__ | [Collider2D.offset](https://docs.unity3d.com/ScriptReference/Collider2D-offset.html)<br />[PolygonCollider2D.GetPath(0)](https://docs.unity3d.com/ScriptReference/PolygonCollider2D.GetPath.html) <br />In classic Unity, the PolygonCollider2D converts an arbitrary outline into multiple primitive convex polygon shapes which themselves are limited in their number of vertices (8). Unfortunately this requires generating a primitive 2D polygon mesh which isn’t currently supported. For now, the PhysicsPolygon primitive supports up to 16 vertices which must form a convex polygon. When the PolygonCollider2D is converted, only the first path is converted and only if it has 16 vertices or less. |

### Collider Conversion to Blobs

GameObject conversion for the Collider2D listed above produces an immutable block of data known as a ``BlobAssetReference``, in this case holding a Collider type.

GameObject conversion will automatically add a collider ECS IComponentData of type ``PhysicsColliderBlob`` as seen here:

```
public struct PhysicsColliderBlob : IComponentData
{
  public BlobAssetReference<Collider> Collider;
}
```

This component will be added alongside the Entity that contains any converted Rigidbody2D components. If there is no Rigidbody2D then the component will be added to the Entity related to the GameObject the Collider2D is on.

#### Compound Collider

The ECS system has a hard limitation that only a single component of any type can be on an Entity. When adding multiple Collider2D either on the same GameObject or on children GameObjects, the physics system will convert and combine them into a single compound collider that contains all the the colliders. This produces a single ``BlobAssetReference<Collider>`` as outlined above.

#### Creating Colliders Manually

You are free to directly create colliders yourself and not use the automatic GameObject conversion system. To do this you populate a specific geometry structure and pass it to the Collider. Create a method for the specific type you wish to create like so:

| Physics Collider Type   | Geometry Type            |
| ----------------------- | ------------------------ |
| PhysicsBoxCollider      | BoxGeometry              |
| PhysicsCapsuleCollider  | CapsuleGeometry          |
| PhysicsCircleCollider   | CircleGeometry           |
| PhysicsPolygonColilder  | PolygonGeometry          |
| PhysicsCompoundCollider | Array of Child Colliders |

For example, create a circle collider and assign it to an Entity with the following script:

```
var collider = PhysicsCircleCollider.Create(
    new CircleGeometry { Center = float2.zero, Radius = 1.5f });

EntityManager.AddComponentData(
    Entity,
    new PhysicsColliderBlob { Collider = collider }
);
```

Ensure to dispose of the memory allocated by a Collider. To have this occur when the Entity is destroyed, add a PhysicsColliderBlobOwner component with the following script:

```
var collider = PhysicsCircleCollider.Create(
    new CircleGeometry { Center = float2.zero, Radius = 1.5f });

EntityManager.AddComponentData(
    Entity,
    new PhysicsColliderBlob { Collider = collider }
);

EntityManager.AddComponentData(
    Entity,
    new PhysicsColliderBlobOwner { Collider = collider }
);
```

To prevent the Collider blob from being automatically disposed, call its Dispose() method instead.

It is often useful to manually create a Collider if you’re using queries that require a specific Collider to be tested against Colliders in the world, such as CastCollider or OverlapCollider queries. In this example, you can create the Collider when a system starts, use it for queries while running, then dispose of the Collider when the system stops.

#### Physics Debug Display

To aid in debugging, add a PhysicsDebugDisplay component to a GameObject in your SubScene. This component has options that allow you to see the Collider outlines, Collider AABB and the Broadphase. You can also configure the colors used to draw them. Note however that this component only works in the Editor and can result in significant performance reduction due to the line drawing used. In the future, this will be significantly improved and even be supported in builds outside the Editor. 

### Adding ECS components directly

All of the ECS components above can be directly added to Entities and do not require GameObject conversion. Simply add the components as described above and you will get identical behaviour.

It should be noted that a [Collider Blob](#heading=h.4hcry2exobbs) (BlobAssetReference<Collider>) defines the exact geometry of a Collider shape only and has no links to the physics body that it is attached to even though the automatic GameObject conversion will produce a new one for each Unity Collider it encounters. This means for example, that you can create a Circle Collider blob and add it to multiple Entities to reuse the same shape and save memory. The automatic GameObject conversion system does not do this kind of optimization.

---

If you have any issues or questions about the 2D Entities package and its features, please visit the [Project Tiny](https://forum.unity.com/forums/project-tiny.151/) forum and [First batch of 2D Features for Project Tiny is now available](https://forum.unity.com/threads/first-batch-of-2d-features-for-project-tiny-is-now-available.830652/) thread for more information and discussions with the development team.