using Unity.Entities;

namespace Unity.U2D.Entities
{
    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
    internal class MaterialProxyConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Material uMaterial) =>
            {
                var proxyEntity = GetPrimaryEntity(uMaterial);
                DstEntityManager.AddSharedComponentData(proxyEntity, new Material2DProxy
                {
                    Material = uMaterial
                });
            });
        }
    }
};