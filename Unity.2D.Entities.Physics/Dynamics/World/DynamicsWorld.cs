using System;
using Unity.Collections;

namespace Unity.U2D.Entities.Physics
{
    public struct DynamicsWorld : IDisposable
    {
        // Body Motion Data/Velocities. The length of these two arrays are always equal.
        private NativeArray<PhysicsBody.MotionData> m_BodyMotionData;
        private NativeArray<PhysicsBody.MotionVelocity> m_BodyMotionVelocity;
        private int m_BodyMotionCount;

        public int BodyMotionCount => m_BodyMotionCount;

        internal void Reset(int bodyMotionCount)
        {
            m_BodyMotionCount = bodyMotionCount;
            if (m_BodyMotionData.Length < m_BodyMotionCount)
            {
                if (m_BodyMotionData.IsCreated)
                {
                    m_BodyMotionData.Dispose();
                }

                m_BodyMotionData = new NativeArray<PhysicsBody.MotionData>(m_BodyMotionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            if (m_BodyMotionVelocity.Length < m_BodyMotionCount)
            {
                if (m_BodyMotionVelocity.IsCreated)
                {
                    m_BodyMotionVelocity.Dispose();
                }

                m_BodyMotionVelocity = new NativeArray<PhysicsBody.MotionVelocity>(m_BodyMotionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
        }

        public NativeSlice<PhysicsBody.MotionData> BodyMotionData => new NativeSlice<PhysicsBody.MotionData>(m_BodyMotionData, 0, m_BodyMotionCount);
        public NativeSlice<PhysicsBody.MotionVelocity> BodyMotionVelocity => new NativeSlice<PhysicsBody.MotionVelocity>(m_BodyMotionVelocity, 0, m_BodyMotionCount);

        public DynamicsWorld(int bodyMotionCount)
        {
            m_BodyMotionCount = bodyMotionCount;
            m_BodyMotionData = new NativeArray<PhysicsBody.MotionData>(bodyMotionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_BodyMotionVelocity = new NativeArray<PhysicsBody.MotionVelocity>(bodyMotionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        #region Cloneable

        public DynamicsWorld Clone()
        {
            return new DynamicsWorld()
            {
                m_BodyMotionCount = m_BodyMotionCount,
                m_BodyMotionData = new NativeArray<PhysicsBody.MotionData>(m_BodyMotionData, Allocator.Persistent),
                m_BodyMotionVelocity = new NativeArray<PhysicsBody.MotionVelocity>(m_BodyMotionVelocity, Allocator.Persistent)
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_BodyMotionCount = 0;

            if (m_BodyMotionData.IsCreated)
            {
                m_BodyMotionData.Dispose();
            }

            if (m_BodyMotionVelocity.IsCreated)
            {
                m_BodyMotionVelocity.Dispose();
            }
        }

        #endregion
    }
}
