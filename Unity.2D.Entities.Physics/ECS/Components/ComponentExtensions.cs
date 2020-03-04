using Unity.Mathematics;

namespace Unity.U2D.Entities.Physics
{
    public static class ComponentExtensions
    {
        // Get the mass, converting from inverse mass.
        public static float GetMass(this PhysicsMass physicsMass)
        {
            if (physicsMass.InverseMass != 0f)
                return math.rcp(physicsMass.InverseMass);

            return 0f;
        }

        // Get the inertia, converting from inverse inertia.
        public static float GetInertia(this PhysicsMass physicsMass)
        {
            if (physicsMass.InverseInertia != 0f)
                return math.rcp(physicsMass.InverseInertia);

            return 0f;
        }

        // Apply a linear impulse to the center of mass.
        public static void ApplyLinearImpulse(ref this PhysicsVelocity physicsVelocity, PhysicsMass physicsMass, float2 impulse)
        {
            physicsVelocity.Linear += impulse * physicsMass.InverseMass;
        }

        // Apply an angular impulse.
        public static void ApplyAngularImpulse(ref this PhysicsVelocity physicsVelocity, PhysicsMass physicsMass, float impulse)
        {
            physicsVelocity.Angular += impulse * physicsMass.InverseInertia;
        }
    }
}
