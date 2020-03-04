using Unity.Entities;
using Unity.Mathematics;

using Color = Unity.Tiny.Color;

namespace Unity.U2D.Entities
{
    internal struct DrawInstructionComparer : System.Collections.Generic.IComparer<DrawInstruction>
    {
        public int Compare(DrawInstruction lhs, DrawInstruction rhs)
        {
            return lhs.DrawOrder - rhs.DrawOrder;
        }
    }

    internal struct DrawInstruction : IBufferElementData
    {
        public Entity Texture;
        public Entity Material;

        public System.IntPtr IndexData;
        public System.IntPtr VertexData;
        public int IndexCount;
        public int VertexCount;

        public Color Color;
        public float4x4 Transform;
        public AABB LocalBounds;

        public int DrawOrder;
        public int SortingLayer;
        public int SortingOrder;
    }
}