#if UNITY_TINY || UNITY_DOTSRUNTIME

using Unity.Entities;
using Unity.Platforms;

namespace Unity.U2D.Entities
{
    internal abstract class ResumableSystemBase : SystemBase
    {
        protected abstract void OnSuspendResume(object sender, SuspendResumeEvent evt);
        
        protected override void OnStartRunning()
        {
#if UNITY_ANDROID
            PlatformEvents.OnSuspendResume += OnSuspendResume;
#endif
        }

        protected override void OnStopRunning()
        {
#if UNITY_ANDROID
            PlatformEvents.OnSuspendResume -= OnSuspendResume;
#endif            
        }        
    }
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME