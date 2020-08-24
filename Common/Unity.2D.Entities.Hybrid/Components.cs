using Unity.Entities;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    internal struct Material2DProxy : ISharedComponentData, System.IEquatable<Material2DProxy>
    {
        public UnityEngine.Material Material;
        public bool Equals(Material2DProxy other)
        {
            return Material == other.Material;
        }
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(Material, null)) hash ^= Material.GetHashCode();
            return hash;
        }
    }
    
    internal struct SpriteProxy : ISharedComponentData, System.IEquatable<SpriteProxy>
    {
        public UnityEngine.Sprite Sprite;
        public bool Equals(SpriteProxy other)
        {
            return Sprite == other.Sprite;
        }
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(Sprite, null)) hash ^= Sprite.GetHashCode();
            return hash;
        }
    }
    
    internal struct HybridRendererInfo
    {
        public int SpriteID;
        public int TextureID;
        public int MaterialID;
        public float4 Color;
        public float4x4 Transform;
        public AABB Bounds;
        public int Layer;
        public int SortingLayer;
        public int SortingOrder;
        public ulong SceneCullingMask;
        public System.IntPtr IndexData;
        public System.IntPtr VertexData;
        public int IndexCount;
        public int VertexCount;
        public int ShaderChannelMask;
    }    
}