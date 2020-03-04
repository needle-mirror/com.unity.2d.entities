using Unity.Entities;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    internal enum RenderItemType
    {
        Sprite,
        Tilemap,
        SpriteShape
    }

    internal struct RenderItem : IBufferElementData
    {
        public RenderItemType Type;
        public Entity Renderer;
        public float4x4 Transform;
        public ulong LayerAndOrder;
        public float SortingDistance;    // For CustomAxisSort;
    }    
}