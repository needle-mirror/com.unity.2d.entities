#if UNITY_TINY || UNITY_DOTSRUNTIME

using Bgfx;
using Unity.Mathematics;
using Unity.Tiny.Rendering;

namespace Unity.U2D.Entities
{
    internal static class SpriteRendererTinyUtils
    {
        private static readonly ulong k_RenderStates = (ulong) (bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA) |
                                                       RendererBGFXStatic.MakeBGFXBlend(
                                                           bgfx.StateFlags.BlendOne,
                                                           bgfx.StateFlags.BlendInvSrcAlpha);
        
        public static uint GetSpriteStencilTest(SpriteMaskInteraction maskInteraction)
        {
            var stencilTest = (uint) 
                              (bgfx.StencilFlags.OpFailSKeep | 
                               bgfx.StencilFlags.OpFailZKeep | 
                               bgfx.StencilFlags.OpPassZKeep) | 
                              TinyUtils.SetStencilRef(1) | 
                              TinyUtils.SetStencilMask(0xFF);

            switch (maskInteraction)
            {
                default:
                case SpriteMaskInteraction.None:
                    stencilTest |= (uint)bgfx.StencilFlags.TestAlways;
                    break;
                case SpriteMaskInteraction.VisibleInsideMask:
                    stencilTest |= (uint) bgfx.StencilFlags.TestLequal;
                    break;                    
                case SpriteMaskInteraction.VisibleOutsideMask:
                    stencilTest |= (uint) bgfx.StencilFlags.TestGreater;
                    break;
            }

            return stencilTest;
        }
        
        public static unsafe void SubmitDrawInstruction(bgfx.Encoder* encoder, float4 color,
            SpriteDefaultShader defaultShader, ushort viewId, 
            SpriteMeshCacheData spriteMesh, uint depth,
            ref float4x4 transform, uint stencilTestMask)
        {
            bgfx.encoder_set_stencil(encoder, stencilTestMask, (uint)bgfx.StencilFlags.None);
            bgfx.encoder_set_state(encoder, k_RenderStates, 0);

            bgfx.encoder_set_index_buffer(encoder, new bgfx.IndexBufferHandle { idx = spriteMesh.IndexBufferHandle }, (uint)0, (uint)spriteMesh.IndexCount);
            bgfx.encoder_set_vertex_buffer(encoder, 0, new bgfx.VertexBufferHandle { idx = spriteMesh.VertexBufferHandle }, 0, (uint)spriteMesh.VertexCount, spriteMesh.VertexLayoutHandle);
            
            fixed (float4x4* p = &transform)
                bgfx.encoder_set_transform(encoder, p, 1);
            
            bgfx.encoder_set_uniform(encoder, defaultShader.TintColorHandle, &color, 1);

            bgfx.encoder_set_texture(encoder, 0, defaultShader.TexColorSamplerHandle, spriteMesh.TextureHandle, System.UInt32.MaxValue);
            bgfx.encoder_submit(encoder, viewId, defaultShader.ProgramHandle, depth, (byte)bgfx.DiscardFlags.All);
        }
    }
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME