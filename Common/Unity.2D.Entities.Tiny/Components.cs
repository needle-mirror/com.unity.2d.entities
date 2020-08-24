#if UNITY_TINY || UNITY_DOTSRUNTIME

using Unity.Entities;

using Bgfx;
using Unity.Jobs;

namespace Unity.U2D.Entities
{
    internal struct VisibleNode
    {
        public Entity Key;
        public ulong LayerAndOrder;
        public uint Depth;
    }
    
    internal unsafe struct EndSubmitJob : IJob
    {
        public bgfx.Encoder* Encoder;
        public void Execute()
        {
            TinyUtils.EndSubmit(Encoder);
        }
    }
    
    internal struct SpriteMeshCacheData : IComponentData
    {
        public Hash128 Hash;
        public bgfx.TextureHandle TextureHandle;
        public ushort IndexBufferHandle;
        public ushort VertexBufferHandle;
        public int IndexCount;
        public int VertexCount;
        public bgfx.VertexLayoutHandle VertexLayoutHandle;
    }  
    
    internal struct SpriteMeshBuffers : ISystemStateComponentData
    {
        public ushort IndexBufferHandle;
        public ushort VertexBufferHandle;
        public int IndexCount;
        public int VertexCount;
        public bgfx.VertexLayoutHandle VertexLayoutHandle;
    }    
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME