using Unity.Entities;
using Unity.Tiny.Rendering;

using Assert = Unity.Tiny.Assertions.Assert;
using bgfx = Bgfx.bgfx;

namespace Unity.U2D.Entities
{
    internal class SpriteShaders
    {
        public SpriteDefaultShader DefaultShader 
        { get { return m_DefaultShader; } }
        private SpriteDefaultShader m_DefaultShader;

        private bool m_IsInitialized = false;

        public void Initialize(EntityManager entityManager, Entity precompiledShadersEntity)
        {
            Assert.IsTrue(precompiledShadersEntity != Entity.Null);
            var shaderData = entityManager.GetComponentData<PrecompiledShaders>(precompiledShadersEntity);

            Assert.IsTrue(shaderData.SpriteShader != Entity.Null);

            var rendererType = bgfx.get_renderer_type();
            m_DefaultShader.Init(entityManager, shaderData.SpriteShader, rendererType);

            m_IsInitialized = true;
        }

        public void Cleanup(bool bgfxInitialized)
        {
            if (!m_IsInitialized)
            { return; }

            if (bgfxInitialized)
            {
                m_DefaultShader.Destroy();
            }
            m_IsInitialized = false;
        }
    }
}
