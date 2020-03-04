using Unity.Entities;
using Unity.U2D.Entities;
using Unity.Mathematics;

using ObjectBounds = Unity.Tiny.Rendering.ObjectBounds;
using Color = Unity.Tiny.Color;
using SpriteRenderer = Unity.U2D.Entities.SpriteRenderer;

namespace Unity.U2D.Conversion
{
    // replace this with a declarative/autodetect system that can solve for dependency requirements
    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
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
    
    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    internal class SpriteRendererConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.SpriteRenderer uSpriteRenderer) =>
            {
                var entity = GetPrimaryEntity(uSpriteRenderer);
                
                DstEntityManager.SetName(entity, "SpriteRenderer: " + uSpriteRenderer.name);
                
                DstEntityManager.AddComponentData(entity, new SpriteRenderer
                {
                    Sprite = GetPrimaryEntity(uSpriteRenderer.sprite),
                    Material = GetPrimaryEntity(uSpriteRenderer.sharedMaterial),
                    Color = new Color(
                        uSpriteRenderer.color.r, 
                        uSpriteRenderer.color.g, 
                        uSpriteRenderer.color.b,
                        uSpriteRenderer.color.a)
                });

                var sortingLayerId = uSpriteRenderer.sortingLayerID;
                DstEntityManager.AddComponentData(entity,
                    new SortLayer
                    {
                        Id = sortingLayerId,
                        Layer = (short) UnityEngine.SortingLayer.GetLayerValueFromID(sortingLayerId),
                        Order = (short) uSpriteRenderer.sortingOrder
                    });
                
                var uWorldToLocalMatrix = uSpriteRenderer.transform.worldToLocalMatrix;
                var worldBounds = uSpriteRenderer.bounds;

                var localBounds = new AABB()
                {
                    Center = uWorldToLocalMatrix.MultiplyPoint(worldBounds.center),
                    Extents = new float3(uSpriteRenderer.size, 1f)
                };
                DstEntityManager.AddComponentData(entity, new ObjectBounds
                {
                    bounds = localBounds
                });
            });
        }
    }
}
