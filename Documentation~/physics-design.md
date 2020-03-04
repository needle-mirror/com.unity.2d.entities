# Design philosophy

The design philosophy of Unity Physics 2D follows that of the [Unity Physics (3D)](https://docs.unity3d.com/Packages/com.unity.physics@0.2/manual/design.html) package. Indeed, much of the code base has been adapted from that package but incorporates specific changes required for 2D simulation. This mostly comes down to different 2D colliders and collision detection algorithms but there are other differences such as type name changes related to its incorporation into another package Unity.U2D.Entities. Additionally it has been designed to work alongside Unity Tiny.  There is however a close relationship between the two and whilst the Unity Physics 2D feature set isn’t complete, it’ll continue to follow closely the features and design of the 3D physics package where possible.

## Stateless

Like Unity Physics (3D), the Unity Physics 2D offered here is also stateless. Modern physics engines maintain large amounts of cached state in order to achieve high performance and simulation robustness. This comes at the cost of added complexity in the simulation pipeline which can be a barrier to modifying code. It also complicates use cases like networking where you may want to roll back and forward physics state. Like Unity Physics (3D), 2D also forgoes this caching in favor of simplicity and control.

Native 2D Physics used the excellent [Box2D](https://box2d.org/) by Erin Catto as its physics engine and we intend to stay compatible with its features and to some degree its behaviours. This will help when transitioning from the native 2D Physics or using both together so that the behaviours are similar. To this end, some aspects of Box2D have been adapted and used within this package and will help later to stay close to the Box2D behaviour. In a pure stateless design, it isn’t possible to keep behaviour identical however we will later extend Unity Physics 2D to include stateful simulation to improve performance but mostly simulation stability (stacking etc).

## Code base structure

| Area       | Description                                                  |
| ---------- | ------------------------------------------------------------ |
| Collision  | Contains all Collision Detection and Spatial Queries.        |
| Containers | Custom containers used by physics. This will later become obsolete. |
| Dynamics   | Contains all simulation code used for integration and later, constraint solving etc. |
| ECS        | Components and Systems for driving physics using the ECS.    |
| Math       | Mathematical functionality used by physics that is not currently contained in Unity.Mathematics. |

