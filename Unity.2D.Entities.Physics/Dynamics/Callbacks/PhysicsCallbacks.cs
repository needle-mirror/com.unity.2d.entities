using System.Collections.Generic;
using Unity.Jobs;

namespace Unity.U2D.Entities.Physics
{
    public class PhysicsCallbacks
    {
        // This should match the number of members in the "Phase" enum below!
        internal static readonly int PhaseCount = 4;
        public enum Phase
        {
            PreBuild = 0,
            PreStepSimulation,
            PostIntegrate,
            PostExport
        }

        public delegate JobHandle Callback(ref PhysicsWorld world, JobHandle inputDeps);

        private struct CallbackAndDependency
        {
            public Callback Callback;
            public JobHandle Dependency;
        }

        private readonly List<CallbackAndDependency>[] m_Callbacks = new List<CallbackAndDependency>[PhaseCount];

        internal PhysicsCallbacks()
        {
            for (int i = 0; i < PhaseCount; ++i)
            {
                m_Callbacks[i] = new List<CallbackAndDependency>(8);
            }
        }

        internal void Enqueue(Phase phase, Callback callback, JobHandle dependency)
        {
            m_Callbacks[(int)phase].Add(new CallbackAndDependency { Callback = callback, Dependency = dependency });
        }

        internal JobHandle ScheduleCallbacksForPhase(Phase phase, ref PhysicsWorld physicsWorld, JobHandle inputDeps)
        {
            ref List<CallbackAndDependency> callbacks = ref m_Callbacks[(int)phase];
            var callbackCount = callbacks.Count;
            if (callbackCount > 0)
            {
                for (var i = 0; i < callbackCount; ++i)
                {
                    var callback = callbacks[i];
                    inputDeps = callback.Callback(ref physicsWorld, JobHandle.CombineDependencies(inputDeps, callback.Dependency));
                }
            }

            return inputDeps;
        }

        internal void Clear()
        {
            for (int i = 0; i < PhaseCount; i++)
            {
                m_Callbacks[i].Clear();
            }
        }
    }
}