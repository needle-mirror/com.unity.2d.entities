using Bgfx;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    internal static class SpriteSubmitHelper
    {
        private static unsafe bgfx.Memory* CreateMemoryBlock(byte* mem, int size)
        {
            return bgfx.copy(mem, (uint)size);
        }        
        
        public static unsafe void SubmitDrawInstruction(SpriteVertexLayout vertexLayout, SpriteShaders spriteShaders, ushort viewId, SpriteVertex* vertices, 
                                                                int vertexCount, ushort* indices, int indexCount, ref float4x4 transform, 
                                                                ref float4 tintColor, bgfx.TextureHandle texture, ulong state)
        {
            SpriteVertex* destVertices = null;
            ushort* destIndices = null;

            bgfx.IndexBufferHandle indexBufferHandle;
            bgfx.VertexBufferHandle vertexBufferHandle;
            
            indexBufferHandle = bgfx.create_index_buffer(CreateMemoryBlock((byte*)indices, indexCount * 2), (ushort)bgfx.BufferFlags.None);
            fixed (bgfx.VertexLayout* declp = vertexLayout.SpriteVertexBufferDecl)
                vertexBufferHandle = bgfx.create_vertex_buffer(CreateMemoryBlock((byte*)vertices, vertexCount * sizeof(SpriteVertex)), declp, (ushort)bgfx.BufferFlags.None);

            var encoder = bgfx.encoder_begin(false);
            bgfx.encoder_set_state(encoder, state, 0);
            bgfx.encoder_set_index_buffer(encoder, indexBufferHandle, 0, (uint)indexCount);
            bgfx.encoder_set_vertex_buffer(encoder, 0, vertexBufferHandle, 0, (uint)vertexCount, vertexLayout.SpriteVertexBufferDeclHandle);            
            
            fixed (float4x4* p = &transform) 
            { bgfx.encoder_set_transform(encoder, p, 1); }
            
            fixed (float4* p = &tintColor)
            { bgfx.encoder_set_uniform(encoder, spriteShaders.DefaultShader.TintColorHandle, p, 1); }
            
            bgfx.encoder_set_texture(encoder, 0, spriteShaders.DefaultShader.TexColorSamplerHandle, texture, System.UInt32.MaxValue);
            bgfx.encoder_submit(encoder, viewId, spriteShaders.DefaultShader.ProgramHandle, 0, false);
            bgfx.encoder_end(encoder);
            
            bgfx.destroy_index_buffer(indexBufferHandle);
            bgfx.destroy_vertex_buffer(vertexBufferHandle);            
        }

        private static unsafe bool AllocateAndSetupVertexAndIndexBuffers(SpriteVertexLayout vertexLayout, bgfx.Encoder* encoder, int indexCount, int vertexCount, SpriteVertex** vertexDataPtr, ushort** indexDataPtr)
        {
            bgfx.TransientIndexBuffer indexBuffer;
            bgfx.TransientVertexBuffer vertexBuffer;
            fixed (bgfx.VertexLayout* declp = vertexLayout.SpriteVertexBufferDecl) 
            {
                if (!bgfx.alloc_transient_buffers(&vertexBuffer, declp, (uint)vertexCount, &indexBuffer, (uint)indexCount)) 
                {
#if DEBUG
                    // TODO: throw or ignore draw? 
                    throw new System.InvalidOperationException("Out of transient bgfx memory!");
#else
                    return false; 
#endif
                }
            }
            bgfx.encoder_set_transient_index_buffer(encoder, &indexBuffer, 0, (uint)indexCount);
            bgfx.encoder_set_transient_vertex_buffer(encoder, 0, &vertexBuffer, 0, (uint)vertexCount, vertexLayout.SpriteVertexBufferDeclHandle);
            *vertexDataPtr = (SpriteVertex*)vertexBuffer.data;
            *indexDataPtr = (ushort*)indexBuffer.data;
            return true;
        }
    }      
}