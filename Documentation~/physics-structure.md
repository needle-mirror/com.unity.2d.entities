# Physics systems structure

The __PhysicsWorldSystem__ is a key system that is the first system executed, and it exposes two very important structures:

* [PhysicsWorld](#physicsworld) (the "PhysicsWorld" property)

* [PhysicsCallbacks](#physicscallbacks) (the "Callbacks" property)

## PhysicsWorld

The PhysicsWorld is built during each update of the __PhysicsWorldSystem__. It can be thought of as the self contained world that will be simulated and that spatial queries can operate on. 

It contains the following and their sub-structures, all of which can be accessed directly:

- CollisionWorld

  - Broadphase (for spatial queries)
  - All PhysicsBody (for quick access to all PhysicsBody by index)

- DynamicsWorld

  * All PhysicsBody motions

- PhysicsSettings

  - World Gravity
  - Thread Control etc.
  
- Properties provide quick access to the PhysicsWorld API

  - AllBodies
- StaticBodies
  - DynamicBodies
  - BodyMotions
  
- Implementation of IQueryable exposing querying of the PhysicsWorld

  - CastRay

    CastCollider

    OverlapPoint

    OverlapCollider

    CalculateDistance (Point)

    CalculateDistance (Collider)

## PhysicsCallbacks

The PhysicsCallbacks allow you to hook into the physics systems at important stages allowing you to perform work. They provide job dependency handles allowing the scheduling of jobs to perform the work so they can be treated as any Update function in a JobComponentSystem. Whilst in some cases, the use of the system update ordering attribute (UpdateBefore/UpdateAfter) is equivalent, certain phases happen within a system update and therefore callbacks provide a mechanism guaranteed to happen at the selected phase also removing the need to know which system performs that phase.

The callback stages currently exposed are:

| Callback Name (Phase) | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| __PreBuild__          | Called by the PhysicsWorldSystem before the PhysicsWorld is built allowing you to ensure your ECS component state is updated prior to building. This is similar to using an “UpdateBefore(typeof(PhysicsWorldSystem))”. |
| __PreStepSimulation__ | Called by the StepPhysicsWorldSystem before the simulation is stepped. At this point, the PhysicsWorld has been built and is ready for querying, modifying etc. |
| __PostIntegrate__     | Called by the StepPhysicsWorldSystem after the simulation has been stepped. At this point, PhysicsBodies have been integrated. |
| __PostExport__        | Called by the ExportPhysicsWorld system after the PhysicsBody poses have been written back to the Transform system. This is effectively the end of the simulation update. |

Note: More callback phases will be exposed later as other features land such as contact generation and solving allowing modification and injection of contacts and constraints etc.

---

If you have any issues or questions about the 2D Entities package and its features, please visit the [Project Tiny](https://forum.unity.com/forums/project-tiny.151/) forum and [First batch of 2D Features for Project Tiny is now available](https://forum.unity.com/threads/first-batch-of-2d-features-for-project-tiny-is-now-available.830652/) thread for more information and discussions with the development team.