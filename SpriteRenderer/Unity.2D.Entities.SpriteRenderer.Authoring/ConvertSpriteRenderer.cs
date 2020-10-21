using Unity.Entities;
using Unity.U2D.Entities;
using Unity.Mathematics;

using SpriteRenderer = Unity.U2D.Entities.SpriteRenderer;

namespace Unity.U2D.Conversion
{
    [ConverterVersion("2d", 3)]
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
    internal class SpriteRendererDeclareAssets : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.SpriteRenderer spriteRenderer) =>
            {
                DeclareReferencedAsset(spriteRenderer.sprite);
                DeclareReferencedAsset(spriteRenderer.sharedMaterial);
            });
        }
    }
    
    [ConverterVersion("2d", 3)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
    internal class SpriteRendererConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.SpriteRenderer uSpriteRenderer) =>
            {
                var entity = GetPrimaryEntity(uSpriteRenderer);
                
                DstEntityManager.SetName(entity, "SpriteRenderer: " + uSpriteRenderer.name);

                var uWorldToLocalMatrix = uSpriteRenderer.transform.worldToLocalMatrix;
                var uWorldBounds = uSpriteRenderer.bounds;
                var localBounds = new AABB()
                {
                    Center = uWorldToLocalMatrix.MultiplyPoint(uWorldBounds.center),
                    Extents = uSpriteRenderer.bounds.extents
                };
                
                var uSortingLayerId = uSpriteRenderer.sortingLayerID;
                var renderingLayer = uSpriteRenderer.gameObject.layer;
                DstEntityManager.AddComponentData(entity, new Renderer2D()
                {
                    RenderingLayer = renderingLayer,
                    SortingLayer = (short) UnityEngine.SortingLayer.GetLayerValueFromID(uSortingLayerId),
                    OrderInLayer = (short) uSpriteRenderer.sortingOrder,
                    Bounds = localBounds,
                });

                DstEntityManager.AddComponentData(entity, new SpriteRenderer
                {
                    Sprite = GetPrimaryEntity(uSpriteRenderer.sprite),
                    Material = GetPrimaryEntity(uSpriteRenderer.sharedMaterial),
                    MaskInteraction = uSpriteRenderer.maskInteraction.ToU2DSpriteMaskInteraction(),
                    Color = new float4(
                        uSpriteRenderer.color.r, 
                        uSpriteRenderer.color.g, 
                        uSpriteRenderer.color.b,
                        uSpriteRenderer.color.a),
                });
            });
        }
    }
}
