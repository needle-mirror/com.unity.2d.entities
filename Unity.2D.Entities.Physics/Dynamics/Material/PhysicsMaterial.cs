using System;
using Unity.Mathematics;

namespace Unity.U2D.Entities.Physics
{
    // Describes how an object should respond to collisions with other objects.
    public struct PhysicsMaterial
    {
        public MaterialFlags Flags;
        public float Friction;
        public float Restitution;
        public CombinePolicy FrictionCombinePolicy;
        public CombinePolicy RestitutionCombinePolicy;

        public bool IsTrigger => (Flags & MaterialFlags.IsTrigger) != 0;

        [Flags]
        public enum MaterialFlags : int
        {
            IsTrigger = 1 << 0,
        }

        // Describes how to mix material properties.
        public enum CombinePolicy
        {
            // sqrt(a * b)
            GeometricMean,

            // min(a, b)
            Minimum,
                       
            // max(a, b)
            Maximum,

            // (a + b) / 2
            ArithmeticMean
        }

        public static readonly PhysicsMaterial Default = new PhysicsMaterial
        {
            Friction = 0.4f,
            Restitution = 0.0f,
            FrictionCombinePolicy = CombinePolicy.GeometricMean,
            RestitutionCombinePolicy = CombinePolicy.Maximum
        };

        // Get a combined friction value for a pair of materials.
        // The combine policy with the highest value takes priority.
        public static float GetCombinedFriction(PhysicsMaterial materialA, PhysicsMaterial materialB)
        {
            var policy = (CombinePolicy)math.max((int)materialA.FrictionCombinePolicy, (int)materialB.FrictionCombinePolicy);
            switch (policy)
            {
                case CombinePolicy.GeometricMean:
                    return math.sqrt(materialA.Friction * materialB.Friction);

                case CombinePolicy.Minimum:
                    return math.min(materialA.Friction, materialB.Friction);

                case CombinePolicy.Maximum:
                    return math.max(materialA.Friction, materialB.Friction);

                case CombinePolicy.ArithmeticMean:
                    return (materialA.Friction + materialB.Friction) * 0.5f;

                default:
                    return 0;
            }
        }

        // Get a combined restitution value for a pair of materials.
        // The combine policy with the highest value takes priority.
        public static float GetCombinedRestitution(PhysicsMaterial materialA, PhysicsMaterial materialB)
        {
            var policy = (CombinePolicy)math.max((int)materialA.RestitutionCombinePolicy, (int)materialB.RestitutionCombinePolicy);

            switch (policy)
            {
                case CombinePolicy.GeometricMean:
                    return math.sqrt(materialA.Restitution * materialB.Restitution);

                case CombinePolicy.Minimum:
                    return math.min(materialA.Restitution, materialB.Restitution);

                case CombinePolicy.Maximum:
                    return math.max(materialA.Restitution, materialB.Restitution);

                case CombinePolicy.ArithmeticMean:
                    return (materialA.Restitution + materialB.Restitution) * 0.5f;

                default:
                    return 0;
            }
        }

        public bool Equals(PhysicsMaterial other)
        {
            return
                Flags == other.Flags &&
                FrictionCombinePolicy == other.FrictionCombinePolicy &&
                RestitutionCombinePolicy == other.RestitutionCombinePolicy &&
                Friction == other.Friction &&
                Restitution == other.Restitution;
        }

        public override int GetHashCode()
        {
            return unchecked((int)math.hash(new uint2(
                unchecked((uint)(
                    (byte)Flags
                    | ((byte)FrictionCombinePolicy << 4)
                    | ((byte)RestitutionCombinePolicy << 8))
                ),
                math.hash(new float2(Friction, Restitution))
            )));
        }
    }
}
