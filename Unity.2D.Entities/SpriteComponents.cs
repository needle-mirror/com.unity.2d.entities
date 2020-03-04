using Unity.Entities;
using Unity.Mathematics;

using Color = Unity.Tiny.Color;

namespace Unity.U2D.Entities
{
    public struct SpriteVertex
    {
        public float3 Position;
        public float2 TexCoord0;
    }

    public struct SortLayer : IComponentData
    {
        /// <summary>
        ///  First, sort by layer.
        /// </summary>
        public short Layer;

        /// <summary>
        ///  If layer values are equal, sort by order.
        /// </summary>
        public short Order;

        /// <summary>
        /// Id that maps to UnityEngine.SortingLayer.id
        /// </summary>
        public int Id;
    }
    
    public struct SpriteMeshData
    {
        public BlobArray<SpriteVertex> Vertices;
        public BlobArray<ushort> Indices;
        public AABB Bounds;
    }

    public struct SpriteRenderData : IComponentData
    {
        public BlobAssetReference<SpriteMeshData> Mesh;
        public Entity Texture;
    }

    public struct SpriteAtlasTexture : IComponentData
    {
        public Entity Texture;
    }

    public struct SpriteAtlasEntry : IBufferElementData
    {
        public BlobAssetReference<SpriteMeshData> Value;
    }

    public struct Sprite : IComponentData
    {
        public int Index;
        public Entity Atlas;
    }

    public struct SpriteRenderer : IComponentData
    {
        public Entity Material;
        public Entity Sprite;
        public Color Color;
    }

    public struct SpriteDefaultMaterial : IComponentData
    {
    }
}
