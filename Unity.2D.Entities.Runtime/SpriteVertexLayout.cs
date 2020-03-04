using Bgfx;

namespace Unity.U2D.Entities
{
    internal class SpriteVertexLayout
    {
        public bgfx.VertexLayoutHandle SpriteVertexBufferDeclHandle 
        { get { return m_spriteVertexBufferDeclHandle; } }
        private bgfx.VertexLayoutHandle m_spriteVertexBufferDeclHandle;
        
        public bgfx.VertexLayout[] SpriteVertexBufferDecl 
        { get { return m_spriteVertexBufferDecl; } }
        private bgfx.VertexLayout[] m_spriteVertexBufferDecl = new bgfx.VertexLayout[8];

        public void Initialize()
        {
            var rendererType = bgfx.get_renderer_type();

            unsafe
            {
                fixed (bgfx.VertexLayout* declp = m_spriteVertexBufferDecl)
                {
                    bgfx.vertex_layout_begin(declp, rendererType);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Position, 3, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_end(declp);
                    m_spriteVertexBufferDeclHandle = bgfx.create_vertex_layout(declp);
                }
            }
        }
    }
}