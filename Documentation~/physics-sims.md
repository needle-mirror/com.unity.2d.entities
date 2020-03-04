# Physics simulation and systems

There are four important systems used to drive the simulation. They are executed in the order they are listed below:

| System                       | Description                                                  |
| ---------------------------- | ------------------------------------------------------------ |
| __PhysicsWorldSystem__       | Produces PhysicsBody that represent the ECS components defining Static, Kinematic and Dynamic bodiesAttaches the Collider to the PhysicsBodyGenerates PhysicsBody Motions ready for the Simulation StepBuilds a Broadphase that represents all the PhysicsBody and Collider in the PhysicsWorld (ready for querying). |
| __StepPhysicsWorldSystem__   | Integrates PhysicsBody motion. PhysicsBodies move with their velocities, velocity damping and gravity is applied. |
| __ExportPhysicsWorldSystem__ | PhysicsBody poses (Translation and Rotation) are exported back to the respective ECS Transform components. |
| __EndFramePhysicsSystem__    | Performs no simulation work but exposes a Job handle that is a combination of all previous physics systems allowing dependency control. |

__Known limitations:__ The physics system does not create contacts and therefore does not solve contacts so there is no collision response or overlap solving. These key features are not yet completed for this early release, and will be included in a future release. However, a full feature set of queries are currently available that can be used in parallel jobs for collision detection and response.

---

If you have any issues or questions about the 2D Entities package and its features, please visit the [Project Tiny](https://forum.unity.com/forums/project-tiny.151/) forum and [First batch of 2D Features for Project Tiny is now available](https://forum.unity.com/threads/first-batch-of-2d-features-for-project-tiny-is-now-available.830652/) thread for more information and discussions with the development team.