using System;

namespace Unity.U2D.Entities.Physics
{
    public static class PhysicsAssert
    {
        public static void IsTrue(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException();
        }

        public static void IsFalse(bool condition)
        {
            if (condition)
                throw new InvalidOperationException();
        }

        public static void AreEqual<A, B>(A value1, B  value2)
            where A : IEquatable<A> where B : IEquatable<B>
        {
            if (!value1.Equals(value2))
                throw new InvalidOperationException();
        }

        public static void AreNotEqual<A, B>(A value1, B  value2)
            where A : IEquatable<A> where B : IEquatable<B>
        {
            if (value1.Equals(value2))
                throw new InvalidOperationException();
        }

    }
}