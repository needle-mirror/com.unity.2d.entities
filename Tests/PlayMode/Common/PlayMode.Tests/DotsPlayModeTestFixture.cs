#if !UNITY_DOTSRUNTIME

using System.Collections;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using UnityEngine;

public class DotsPlayModeTestFixture
{ 
    protected static World World;
    protected static EntityManager EntityManager => World.EntityManager;

    private TimeData m_TimeData;
    
    [SetUp]
    public virtual void Setup()
    {
        if (World != null) 
            return;
        
        World = DefaultWorldInitialization.Initialize("Test World");
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World, DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default));
    }

    [TearDown]
    public virtual void TearDown()
    {
        if (World == null)
            return;
        
        World.Dispose();
        World = null;
    }    
    
    protected IEnumerator MainLoop(int count = 1)
    {
        var timeData = StepWallRealtimeFrame(0.1);
        for (var c = 0; c < count; ++c)
        {
            EntityManager.World.SetTime(timeData);
            EntityManager.World.Update();
            yield return new WaitForFixedUpdate();
        }
    }
    
    private TimeData StepWallRealtimeFrame(double deltaTimeDouble)
    {
        var frameDeltaTime = (float)deltaTimeDouble;

        if (frameDeltaTime >= .5f) // max 1/2 second
            frameDeltaTime = .5f;
        if (frameDeltaTime <= 0.0) // no negative steps
            return m_TimeData;

        m_TimeData = new TimeData(
            elapsedTime: m_TimeData.ElapsedTime + frameDeltaTime,
            deltaTime: frameDeltaTime);

        return m_TimeData;
    }    

    protected static bool ApproxEqual(float a, float b)
    {
        const float eps = 0.001f;
        float d = a - b;
        return d > -eps && d < eps;
    }
}

#endif //!UNITY_DOTSRUNTIME