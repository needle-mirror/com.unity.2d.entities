using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

namespace Unity.U2D.Entities
{
    [AlwaysUpdateSystem]
    [UnityEngine.ExecuteAlways]
    [AlwaysSynchronizeSystem]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal class SpriteRendererHybridRendering : SystemBase
    {
        private const int k_ShaderChannelMaskVertex = 1 << (int) UnityEngine.Rendering.VertexAttribute.Position;
        private const int k_ShaderChannelMaskTexCoord0 = 1 << (int) UnityEngine.Rendering.VertexAttribute.TexCoord0;
        private const int k_SpriteChannelMask = k_ShaderChannelMaskVertex | k_ShaderChannelMaskTexCoord0;

        private EntityQuery m_RendererQuery;

        protected override void OnCreate()
        {
            m_RendererQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Renderer2D>(),
                    ComponentType.ReadOnly<SpriteRenderer>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                }
            });
        }

        protected override void OnUpdate()
        {
#if UNITY_EDITOR               
            // Added since the systems might run while the editor is compiling
            if (UnityEditor.EditorApplication.isCompiling)
                return;
#endif //UNITY_EDITOR
            
            CommonHybridUtils.ClearSpriteRendererGroup();

            var entityCount = m_RendererQuery.CalculateEntityCount();
            var renderers = new NativeList<HybridRendererInfo>(entityCount, Allocator.TempJob);

            Entities
                .WithoutBurst()
                .ForEach((Entity e,
                int entityInQueryIndex,
                in Renderer2D renderer2D,
                in SpriteRenderer spriteRenderer,
                in LocalToWorld localToWorld) =>
            {
                if (!SpriteRendererUtils.IsValidSpriteRenderer(spriteRenderer))
                    return;
                if (!SpriteRendererHybridUtils.IsValidHybridSpriteRenderer(EntityManager, spriteRenderer))
                    return; 
#if UNITY_EDITOR
                var editorRenderData = EntityManager.GetSharedComponentData<EditorRenderData>(e);
#endif //UNITY_EDITOR
                
                var materialData = EntityManager.GetSharedComponentData<Material2DProxy>(spriteRenderer.Material);
                var spriteProxy = EntityManager.GetSharedComponentData<SpriteProxy>(spriteRenderer.Sprite);

                renderers.Add(new HybridRendererInfo
                {
                    SpriteID = spriteProxy.Sprite.GetInstanceID(),
                    TextureID = spriteProxy.Sprite.texture.GetInstanceID(),
                    MaterialID = materialData.Material.GetInstanceID(),

                    Color = spriteRenderer.Color,
                    Transform = localToWorld.Value,
                    Bounds = renderer2D.Bounds,

                    Layer = renderer2D.RenderingLayer,
                    SortingLayer = renderer2D.SortingLayer,
                    SortingOrder = renderer2D.OrderInLayer,
#if UNITY_EDITOR
                    SceneCullingMask = editorRenderData.SceneCullingMask,
#endif //UNITY_EDITOR

                    ShaderChannelMask = k_SpriteChannelMask
                });
            }).Run();
            // Burst turned off because we are working with managed objects

            CommonHybridUtils.AddToSpriteRendererGroup(renderers.AsArray());
            renderers.Dispose();
        }
    }
}