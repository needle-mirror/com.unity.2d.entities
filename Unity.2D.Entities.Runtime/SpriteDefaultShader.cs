using Bgfx;
using Unity.Tiny.Rendering;
using Unity.Entities;

namespace Unity.U2D.Entities
{
    internal struct SpriteDefaultShader
    {
        public bgfx.ProgramHandle ProgramHandle { get; private set; }
        public bgfx.UniformHandle TexColorSamplerHandle { get; private set; }
        public bgfx.UniformHandle TintColorHandle { get; private set; }

        public void Init(EntityManager entityMgr, Entity shaderDataEntity, bgfx.RendererType backend)
        {
            unsafe
            {
                var fsl = 0;
                var vsl = 0;
                byte* fsPtr = null;
                byte* vsPtr = null;
                BGFXShaderHelper.GetPrecompiledShaderData(entityMgr, shaderDataEntity, backend, ref fsPtr, out fsl, ref vsPtr, out vsl);

                ProgramHandle = BGFXShaderHelper.MakeProgram(backend, fsPtr, fsl, vsPtr, vsl, "sprite");
            }            

            TexColorSamplerHandle = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
            TintColorHandle = bgfx.create_uniform("u_tint0", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            bgfx.destroy_program(ProgramHandle);
            bgfx.destroy_uniform(TexColorSamplerHandle);
            bgfx.destroy_uniform(TintColorHandle);
        }
    }
}