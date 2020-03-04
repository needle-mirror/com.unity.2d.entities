using UnityEngine;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

class BaseLegacyConversionTestFixture
{
    protected const float Epsilon = 1e-3f;

    protected GameObject Root { get; set; }
    protected GameObject Child { get; private set; }
    protected BlobAssetStore BlobStore { get; set; }
    protected World World { get; set; }
    protected EntityManager EntityManager => World.EntityManager;

    [SetUp]
    protected virtual void Setup()
    {
        World = new World("Test Physics World");
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

        CreateLegacyComponent<TRoot>(Root);
        CreateLegacyComponent<TChild>(Child);
    }

    protected GameObject CreateChild(float2 translation)
    {
        var gameObject = new GameObject();
        gameObject.transform.SetParent(Root.transform);
        gameObject.transform.localPosition = new Vector3(translation.x, translation.y, 0f);
        return gameObject;
    }

    protected T CreateLegacyComponent<T>(GameObject gameObject) where T : Component => gameObject.AddComponent<T>();
    protected bool HasComponent<T>(Entity entity) where T : struct, IComponentData => EntityManager.HasComponent<T>(entity);
    protected T GetComponentData<T>(Entity entity) where T : struct, IComponentData => EntityManager.GetComponentData<T>(entity);

    protected void RunConversion(GameObject gameObject)
    {
        var settings = GameObjectConversionSettings.FromWorld(World, BlobStore);
        GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObject, settings);
    }
}
