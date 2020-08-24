#if UNITY_TINY || UNITY_DOTSRUNTIME

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Tiny;
using Unity.Tiny.Rendering;
using Unity.Transforms;
using Unity.Platforms;

using DisplayInfo = Unity.Tiny.DisplayInfo;

namespace Unity.U2D.Entities
{
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RenderNodeSystem))]
    [UpdateAfter(typeof(ShaderSystem))]
    internal class SpriteRendererTinyRendering : ResumableSystemBase
    {
        public SpriteDefaultShader DefaultShader => (SpriteDefaultShader)m_DefaultShader;
        private IShader2D m_DefaultShader = new SpriteDefaultShader();
        private EntityQuery m_CameraQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new []
                {
                    ComponentType.ReadOnly<Camera>(),
                }
            });
        }
        
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            RequireSingletonForUpdate<DisplayInfo>();
            RegisterShader();
        }

        protected override void OnSuspendResume(object sender, SuspendResumeEvent evt)
        {
            if (!evt.Suspend)
                return;

            m_DefaultShader = new SpriteDefaultShader();
            RegisterShader();
        }

        private void RegisterShader()
        {
            var shaderSystem = World.GetOrCreateSystem<ShaderSystem>();
            shaderSystem.RegisterShader(m_DefaultShader); // boxes it
        }

        protected override void OnUpdate()
        {
            if (!m_DefaultShader.IsInitialized)
                return;

            var hashMap = new NativeHashMap<Entity, ushort>(1, Allocator.TempJob);
            Entities
                .WithName("LocateAndStoreSpritePass")
                .ForEach((
                    in RenderPass renderPass,
                    in RenderPassUpdateFromCamera renderPassUpdateFromCamera) =>
                {
                    if (renderPass.passType != RenderPassType.Sprites)
                        return;
            
                    hashMap.Add(renderPassUpdateFromCamera.camera, renderPass.viewId);
                }).Run();
            
            var di = GetSingleton<DisplayInfo>();
            var shader = (SpriteDefaultShader)m_DefaultShader;
            
            var renderNodeSystem = World.GetExistingSystem<RenderNodeSystem>();
            
            using (var cameraEntities = m_CameraQuery.ToEntityArray(Allocator.TempJob))
            using (var jobHandles = new NativeList<JobHandle>(Allocator.TempJob))
            {
                for (var i = 0; i < cameraEntities.Length; i++)
                {
                    var cameraEntity = cameraEntities[i];
                    
                    if (!hashMap.ContainsKey(cameraEntity))
                        return;
                    
                    var passViewId = hashMap[cameraEntity];
                    var visibleList = renderNodeSystem.GetOrderedVisibleMap(cameraEntity);
                    
                    unsafe
                    {
                        var encoder = TinyUtils.BeginSubmit();
                    
                        var renderJob = Entities
                            .WithReadOnly(visibleList)
                            .ForEach((Entity e, 
                                in SpriteMeshCacheData rd, 
                                in Renderer2D renderer2D, in SpriteRenderer sr, in LocalToWorld transform) =>
                            {
                                if (!visibleList.TryGetValue(e, out var renderNode))
                                    return; 
                                
                                var tintColor = di.colorSpace == ColorSpace.Gamma
                                    ? sr.Color
                                    : Color.SRGBToLinear(sr.Color);
                                
                                var stencilTestMask = SpriteRendererTinyUtils.GetSpriteStencilTest(sr.MaskInteraction);
                                
                                var localToWorld = transform;
                                SpriteRendererTinyUtils.SubmitDrawInstruction(encoder, tintColor,
                                    shader, passViewId,
                                    rd, renderNode.Depth,
                                    ref localToWorld.Value,
                                    stencilTestMask);
                                
                            }).Schedule(Dependency);
                    
                        var endSubmitJob = new EndSubmitJob
                        {
                            Encoder = encoder
                        }.Schedule(renderJob);
                        
                        jobHandles.Add(endSubmitJob);
                    }
                }
                Dependency = JobHandle.CombineDependencies(jobHandles.AsArray());
                hashMap.Dispose();
            }    
        }
    }
}

#endif //UNITY_TINY || UNITY_DOTSRUNTIME