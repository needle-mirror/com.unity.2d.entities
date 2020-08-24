using System.Collections;
using UnityEngine;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    internal class DotsEditModeTestFixture
    {
        protected const float Epsilon = 1e-3f;
        private TimeData m_TimeData;

        protected GameObject Root { get; set; }
        protected GameObject Child { get; private set; }
        protected BlobAssetStore BlobStore { get; set; }
        protected World World { get; set; }
        protected EntityManager EntityManager => World.EntityManager;

        [SetUp]
        protected virtual void Setup()
        {
            World = DefaultWorldInitialization.Initialize("Test World");
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World,
                DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Editor));

            BlobStore = new BlobAssetStore();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            if (Root != null)
            {
                GameObject.DestroyImmediate(Root);
                Root = null;
            }

            Child = null;

            BlobStore.Dispose();
            World.Dispose();
        }

        protected void CreateHierarchy<TRoot, TChild>()
            where TRoot : Component
            where TChild : Component
        {
            Assert.IsTrue(Root == null && Child == null);

            Root = new GameObject();
            Child = new GameObject();
            Child.transform.SetParent(Root.transform);

            CreateClassicComponent<TRoot>(Root);
            CreateClassicComponent<TChild>(Child);
        }

        protected GameObject CreateChild(float2 translation)
        {
            var gameObject = new GameObject();
            gameObject.transform.SetParent(Root.transform);
            gameObject.transform.localPosition = new Vector3(translation.x, translation.y, 0f);
            return gameObject;
        }

        protected T CreateClassicComponent<T>(GameObject gameObject) where T : Component =>
            gameObject.AddComponent<T>();

        protected bool HasComponent<T>(Entity entity) where T : struct, IComponentData =>
            EntityManager.HasComponent<T>(entity);

        protected T GetComponentData<T>(Entity entity) where T : struct, IComponentData =>
            EntityManager.GetComponentData<T>(entity);

        protected bool RunConversion(GameObject gameObject)
        {
            var settings = GameObjectConversionSettings.FromWorld(World, BlobStore);
            settings.FilterFlags = WorldSystemFilterFlags.HybridGameObjectConversion;

            var convertedEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObject, settings);

            var wasConversionSuccessful = convertedEntity != Entity.Null;
            return wasConversionSuccessful;
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

        protected void CleanupWorld()
        {
            var allEntities = EntityManager.GetAllEntities();
            EntityManager.DestroyEntity(allEntities);
        }

        private TimeData StepWallRealtimeFrame(double deltaTimeDouble)
        {
            var frameDeltaTime = (float) deltaTimeDouble;

            if (frameDeltaTime >= .5f) // max 1/2 second
                frameDeltaTime = .5f;
            if (frameDeltaTime <= 0.0) // no negative steps
                return m_TimeData;

            m_TimeData = new TimeData(
                elapsedTime: m_TimeData.ElapsedTime + frameDeltaTime,
                deltaTime: frameDeltaTime);

            return m_TimeData;
        }
    }
}