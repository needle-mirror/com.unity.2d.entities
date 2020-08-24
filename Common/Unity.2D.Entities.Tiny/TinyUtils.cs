#if UNITY_TINY || UNITY_DOTSRUNTIME

using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny.Rendering;
using bgfx = Bgfx.bgfx;

namespace Unity.U2D.Entities
{
    internal static class TinyUtils
    {
        public static ulong MergeLayerAndOrder(short layer, short order, float z)
        {
            // Make sure negative values are capped as shorts
            var ulLayer = (ulong)(ushort)layer;
            var ulOrder = (ulong)(ushort)order;
                
            var unsignedZ = math.asuint(z);
                
            // Fix up twos complement for negative floats
            unsignedZ ^= (uint)((int)unsignedZ >> 31) >> 1; 
                
            // Pack and fixup signed values for sort
            var packed = ulLayer << 48 | ulOrder << 32 | (ulong)unsignedZ;
            return packed ^ 0x8000_8000_80000000ul;
            // from trunk
            //(UInt32(SInt32(layer) + (1 << 15)) << 16) | UInt32(SInt32(order) + (1 << 15));
        }         
        
        public static bool ExtractCacheData(EntityManager em, Entity spriteEntity, out SpriteMeshCacheData cacheData)
        {
            cacheData = default;

            if (!em.HasComponent<SpriteRenderData>(spriteEntity))
                return false;
            var srd = em.GetComponentData<SpriteRenderData>(spriteEntity);
                                
            if (!em.HasComponent<TextureBGFX>(srd.Texture))
                return false;
            if (!em.HasComponent<SpriteMeshBuffers>(spriteEntity))
                return false;
                                
            var spriteMesh = em.GetComponentData<SpriteMeshBuffers>(spriteEntity);
            var texture = em.GetComponentData<TextureBGFX>(srd.Texture);
                
            cacheData = new SpriteMeshCacheData
            {
                Hash = new Hash128((uint)spriteEntity.Index, (uint)spriteEntity.Version, 0 , 0),
                TextureHandle = texture.handle,
                IndexBufferHandle = spriteMesh.IndexBufferHandle,
                VertexBufferHandle = spriteMesh.VertexBufferHandle,
                IndexCount = spriteMesh.IndexCount,
                VertexCount = spriteMesh.VertexCount,
                VertexLayoutHandle = spriteMesh.VertexLayoutHandle,
            };

            return true;
        }        
        
        public static uint SetStencilRef(int value)
        {
            return ((uint)value << (int)bgfx.StencilFlags.FuncRefShift) & (int)bgfx.StencilFlags.FuncRefMask;
        }

        public static uint SetStencilMask(int value)
        {
            return ((uint)value << (int)bgfx.StencilFlags.FuncRmaskShift) & (int)bgfx.StencilFlags.FuncRmaskMask;
        }    

        public static unsafe bgfx.Encoder* BeginSubmit()
        {
            return bgfx.encoder_begin(false);
        }
        public static unsafe void EndSubmit(bgfx.Encoder* encoder)
        {
            bgfx.encoder_end(encoder);
        }
    }
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME