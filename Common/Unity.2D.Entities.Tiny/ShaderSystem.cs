#if UNITY_TINY || UNITY_DOTSRUNTIME

using System.Collections.Generic;
using Unity.Entities;
using Unity.Tiny.Rendering;
using Unity.Platforms;

using bgfx = Bgfx.bgfx;

namespace Unity.U2D.Entities
{
    public interface IShader2D
    {
        Hash128 Guid { get; }
        bool IsInitialized { get; }
        void Init(bgfx.ProgramHandle programHandle);
        void Destroy();
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
#if UNITY_DOTSRUNTIME    
    [UpdateAfter(typeof(RendererBGFXSystem))]
#endif // UNITY_DOTSRUNTIME
    internal class ShaderSystem : ResumableSystemBase
    {
        private List<IShader2D> m_Shaders;
        public void RegisterShader(IShader2D shader)
        {
            m_Shaders.Add(shader);
        }
        
        private bool IsNativeRendererInitialized()
        {
            var system = World.GetExistingSystem<RendererBGFXSystem>();
            return system?.IsInitialized() ?? false;
        }
        
        protected override void OnCreate()
        {
            m_Shaders = new List<IShader2D>();
        }

        protected override void OnDestroy()
        {
            ClearShaders();
        }

        protected override void OnSuspendResume(object sender, SuspendResumeEvent evt)
        {
            if (!evt.Suspend)
                return;

            ClearShaders();            
        }

        private void ClearShaders()
        {
            foreach (var shader in m_Shaders)
                    shader.Destroy();
            
            m_Shaders.Clear();
        }

        protected override void OnUpdate()
        {
            if (!IsNativeRendererInitialized())
                return;
            
            var rendererType = bgfx.get_renderer_type();

            Entities
                .WithName("InitializeDefaultShader")
                .WithoutBurst()
                .ForEach((Entity e, ref PrecompiledShader bgfxShader, in VertexShaderBinData vertexBinData, in FragmentShaderBinData fragmentBinData) =>
                {
                    for(var i = 0; i < m_Shaders.Count; i++)
                    {
                        if (m_Shaders[i].IsInitialized)
                            continue;
                        
                        if (m_Shaders[i].Guid == bgfxShader.Guid)
                            m_Shaders[i].Init(BGFXShaderHelper.GetPrecompiledShaderData(rendererType, vertexBinData, fragmentBinData, ref bgfxShader.Name));
                    }
                }).Run();

        }
    }
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME