#if UNITY_TINY || UNITY_DOTSRUNTIME

using Bgfx;
using Unity.Entities; 
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.U2D.Entities
{
    internal struct SpriteDefaultShader : IShader2D
    {
        public bgfx.ProgramHandle ProgramHandle { get; private set; }
        public bgfx.UniformHandle TexColorSamplerHandle { get; private set; }
        public bgfx.UniformHandle TintColorHandle { get; private set; }
        public bgfx.VertexLayoutHandle LayoutHandle { get; private set; }
        public NativeArray<bgfx.VertexLayout> VertexLayout { get; private set; }
        
        public static Hash128 Guid => new Hash128("8FB548888C9446179518348C6AE7E9E0");

        Hash128 IShader2D.Guid => SpriteDefaultShader.Guid;
        public bool IsInitialized => VertexLayout.IsCreated;

        public void Init(bgfx.ProgramHandle programHandle)
        {
            ProgramHandle = programHandle;
            TexColorSamplerHandle = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
            TintColorHandle = bgfx.create_uniform("u_tint0", bgfx.UniformType.Vec4, 1);
            
            unsafe // default vertex layout
            {
                var rendererType = bgfx.get_renderer_type();
                VertexLayout = new NativeArray<bgfx.VertexLayout>(8, Allocator.Persistent);

                var layoutPtr = (bgfx.VertexLayout*) VertexLayout.GetUnsafePtr();
                bgfx.vertex_layout_begin(layoutPtr, rendererType);
                bgfx.vertex_layout_add(layoutPtr, bgfx.Attrib.Position, 3, bgfx.AttribType.Float, false, false);
                bgfx.vertex_layout_add(layoutPtr, bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float, false, false);
                bgfx.vertex_layout_end(layoutPtr);
            
                LayoutHandle = bgfx.create_vertex_layout(layoutPtr);                
            }
        }

        public void Destroy()
        {
            if (!IsInitialized)
                return;
            
            VertexLayout.Dispose();
            bgfx.destroy_program(ProgramHandle);
            bgfx.destroy_uniform(TexColorSamplerHandle);
        }
    }
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME