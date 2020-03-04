using Unity.Entities;
using DotsRuntimeBuildProfile = Unity.Entities.Runtime.Build.DotsRuntimeBuildProfile;

namespace Unity.U2D.Entities
{
    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    internal class TextureProxyConversion : GameObjectConversionSystem
    {
        public override bool ShouldRunConversionSystem()
        {
            return !TryGetBuildSettingsComponent<DotsRuntimeBuildProfile>(out var _);
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Sprite sprite) =>
            {
                var proxyEntity = GetPrimaryEntity(sprite);
                DstEntityManager.AddSharedComponentData(proxyEntity, new SpriteProxy
                {
                    Sprite = sprite
                });                
            });
        }
    }

    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    internal class MaterialProxyConversion : GameObjectConversionSystem
    {
        public override bool ShouldRunConversionSystem()
        {
            return !TryGetBuildSettingsComponent<DotsRuntimeBuildProfile>(out var _);
        }       
        
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Material uMaterial) =>
            {
                var proxyEntity = GetPrimaryEntity(uMaterial);
                DstEntityManager.AddSharedComponentData(proxyEntity, new MaterialProxy
                {
                    Material = uMaterial
                });
            });
        }
    }

    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    internal class SpriteMaterialConversion : GameObjectConversionSystem
    {
        private const string k_SpriteDefault = "Sprites/Default";
        private const string k_SpriteLitDefault = "Universal Render Pipeline/2D/Sprite-Lit-Default";

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, UnityEngine.Material uMaterial) =>
            {
                switch (uMaterial.shader.name)
                {
                    case k_SpriteDefault:
                    case k_SpriteLitDefault:
                        Entity primaryEntity = GetPrimaryEntity(uMaterial);
                        ConvertSpriteDefaultMaterialToDots(primaryEntity);
                        break;
                }
            });
        }

        private void ConvertSpriteDefaultMaterialToDots(Entity entity)
        {
            DstEntityManager.SetName(entity, "Material: " + k_SpriteDefault);
            DstEntityManager.AddComponent<SpriteDefaultMaterial>(entity);
        } 
    }    
};