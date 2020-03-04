using Unity.Collections;
using Unity.Entities;
using Unity.Tiny.Rendering;
using Bgfx;
using Unity.Jobs;
using Unity.Platforms;

namespace Unity.U2D.Entities
{   
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(CombineDrawCallSystem))]
    internal class SpriteRuntimeRendering : JobComponentSystem
    {
        private bool bgfxInitialized
        {
            get
            {
                var system = World.GetExistingSystem<RendererBGFXSystem>();
                return system?.m_initialized ?? false;
            }
        }

        private EntityQuery m_RenderPassQuery;
        private SpriteVertexLayout m_VertexLayout;
        private SpriteShaders m_SpriteShaders;
        private bool m_Resume = false;

        protected override void OnCreate()
        {
            m_VertexLayout = new SpriteVertexLayout();
            m_SpriteShaders = new SpriteShaders();
            
            m_RenderPassQuery = GetEntityQuery(ComponentType.ReadOnly<RenderPass>());
            RequireSingletonForUpdate<Unity.Tiny.DisplayInfo>();
        }

        protected override void OnStartRunning()
        {
            if (!bgfxInitialized)
            { return; }

            m_VertexLayout.Initialize();
            m_SpriteShaders.Initialize(EntityManager, GetSingletonEntity<PrecompiledShaders>());
#if (UNITY_ANDROID)
            PlatformEvents.OnSuspendResume += OnSuspendResume;
#endif
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!bgfxInitialized)
            { return inputDeps; }

            if (m_Resume)
            {
                m_SpriteShaders.Initialize(EntityManager, GetSingletonEntity<PrecompiledShaders>());
                m_Resume = false;
            }

            var renderPassEntries = m_RenderPassQuery.ToEntityArray(Allocator.TempJob);
            var di = GetSingleton<Unity.Tiny.DisplayInfo>();
            
            // TODO fetch cameras sprite pass
            var spritePass = new RenderPass();
            for (var i = 0; i < renderPassEntries.Length; i++)
            {
                var renderPass = EntityManager.GetComponentData<RenderPass>(renderPassEntries[i]);
                if (renderPass.passType != RenderPassType.Sprites) 
                { continue; }
                
                spritePass = renderPass;
                break;
            }

            var renderState = (ulong)(bgfx.StateFlags.WriteRgb 
                | bgfx.StateFlags.WriteA) 
                | RendererBGFXSystem.MakeBGFXBlend(bgfx.StateFlags.BlendOne, bgfx.StateFlags.BlendInvSrcAlpha);
            
            inputDeps.Complete();

            Entities
                .WithoutBurst()
                .ForEach((Entity e,
                DynamicBuffer<DrawInstruction> drawInstructions) =>
                {
                    for (var i = 0; i < drawInstructions.Length; i++)
                    {
                        var textureEntry = drawInstructions[i].Texture;
                        if (!EntityManager.HasComponent<TextureBGFX>(textureEntry))
                        {
                            continue;
                        }

                        var textureBgfx = EntityManager.GetComponentData<TextureBGFX>(textureEntry);

                        unsafe
                        {
                            var vertexData = (SpriteVertex*)drawInstructions[i].VertexData;
                            var vertexCount = drawInstructions[i].VertexCount;
                            var indexData = (ushort*)drawInstructions[i].IndexData;
                            var indexCount = drawInstructions[i].IndexCount;
                            var transform = drawInstructions[i].Transform;
                            var tintColor = di.disableSRGB ? drawInstructions[i].Color.AsFloat4() : drawInstructions[i].Color.ToLinear();
                            var textureHandle = textureBgfx.handle;

                            SpriteSubmitHelper.SubmitDrawInstruction(
                                m_VertexLayout,
                                m_SpriteShaders,
                                spritePass.viewId,
                                vertexData,
                                vertexCount,
                                indexData,
                                indexCount,
                                ref transform,
                                ref tintColor,
                                textureHandle,
                                renderState);
                        }
                    }

                    drawInstructions.Clear();
                }).Run();
            
            renderPassEntries.Dispose();

            return inputDeps;
        }

        private void OnSuspendResume(object sender, SuspendResumeEvent evt)
        {
            if (evt.Suspend)
            {
                m_SpriteShaders.Cleanup(bgfxInitialized);
            }
            else
            {
                m_Resume = true;
            }
        }

        protected override void OnStopRunning()
        {
            PlatformEvents.OnSuspendResume -= OnSuspendResume;
        }

        protected override void OnDestroy()
        {
            m_SpriteShaders.Cleanup(false);
        }
    }
}
