using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using UnityEngine;

public class DotsPlayModeTestFixture
{ 
    protected static World World;
    protected static EntityManager EntityManager => World.EntityManager;

    TimeData m_TimeData;
    NativeLeakDetectionMode OldLeakMode;
    
    [SetUp]
    public virtual void Setup()
    {
        // Ensure we have full stack traces when running tests.
        OldLeakMode = NativeLeakDetection.Mode;
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
        
        if (World != null) 
            return;
        
        World = DefaultWorldInitialization.Initialize("Test World");
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World, DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default));
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Restore the old leak mode.
        NativeLeakDetection.Mode = OldLeakMode;
        
        if (World == null)
            return;
        
        World.Dispose();
        World = null;
    }    
    
    protected void MainLoop(int count = 1)
    {
        var timeData = StepWallRealtimeFrame(0.1);
        for (var c = 0; c < count; ++c)
        {
            EntityManager.World.SetTime(timeData);
            EntityManager.World.Update();
        }
    }
    
    TimeData StepWallRealtimeFrame(double deltaTimeDouble)
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
