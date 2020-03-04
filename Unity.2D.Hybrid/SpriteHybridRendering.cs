using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Tiny.Rendering;
using Unity.Transforms;
using UnityEngine.Experimental.U2D;
using UnityEngine.SceneManagement;

namespace Unity.U2D.Entities
{
    
#if UNITY_EDITOR
    
    [UnityEditor.InitializeOnLoad]
    internal static class OnExitClear
    {
        static OnExitClear()
        {
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                    change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    SpriteRendererGroup.Clear();
                }
            };
            
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += OnSceneOpening;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        private static void OnSceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            SpriteRendererGroup.Clear();
        }
        private static void OnSceneClosing(Scene scene, bool close)
        {
            SpriteRendererGroup.Clear();
        }

        private static void OnSceneChanged(Scene a, Scene b)
        {
            SpriteRendererGroup.Clear();
        }
    }
    
#endif    

    internal struct MaterialProxy : ISharedComponentData, System.IEquatable<MaterialProxy>
    {
        public UnityEngine.Material Material;
        public bool Equals(MaterialProxy other)
        {
            return Material == other.Material;
        }
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(Material, null)) hash ^= Material.GetHashCode();
            return hash;
        }
    }

    internal struct Texture2DProxy : ISharedComponentData, System.IEquatable<Texture2DProxy>
    {
        public UnityEngine.Texture2D Texture;
        public bool Equals(Texture2DProxy other)
        {
            return Texture == other.Texture;
        }
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(Texture, null)) hash ^= Texture.GetHashCode();
            return hash;
        }
    }
    
    internal struct SpriteProxy : ISharedComponentData, System.IEquatable<SpriteProxy>
    {
        public UnityEngine.Sprite Sprite;
        public bool Equals(SpriteProxy other)
        {
            return Sprite == other.Sprite;
        }
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(Sprite, null)) hash ^= Sprite.GetHashCode();
            return hash;
        }
    }    

    [AlwaysUpdateSystem]
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal class SpriteHybridRendering : JobComponentSystem
    {
        private EntityQuery m_RendererQuery;

        protected override void OnCreate()
        {
            m_RendererQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<SpriteRenderer>(),
                    ComponentType.ReadOnly<ObjectBounds>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<SortLayer>(),
                }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Added since the systems might run while the editor is compiling
            if (UnityEditor.EditorApplication.isCompiling)
            {
                return inputDeps; 
            }
            
            var kShaderChannelMaskVertex = 1 << (int)UnityEngine.Rendering.VertexAttribute.Position;
            var kShaderChannelMaskTexCoord0 = 1 << (int) UnityEngine.Rendering.VertexAttribute.TexCoord0;
            var kSpriteChannelMask = kShaderChannelMaskVertex | kShaderChannelMaskTexCoord0;
            
            SpriteRendererGroup.Clear();
            inputDeps.Complete();

            var entityCount = m_RendererQuery.CalculateEntityCount();
            var renderers = new NativeList<SpriteIntermediateRendererInfo>(entityCount, Allocator.TempJob);

            unsafe
            {
                Entities.ForEach((Entity e,
                    ref SpriteRenderer spriteRenderer,
                    ref ObjectBounds localBounds,
                    ref LocalToWorld localToWorld,
                    ref SortLayer sortLayer) =>
                {
                    // Skip sprite renderers without any sprite or material
                    // Can happen if no sprite/material has been selected in the SpriteRender's inspector 
                    if (spriteRenderer.Sprite == Entity.Null ||
                        spriteRenderer.Material == Entity.Null)
                    {
                        return;
                    }
                    
                    var spriteData = EntityManager.GetComponentData<SpriteRenderData>(spriteRenderer.Sprite);
                    var meshData = spriteData.Mesh;

                    var editorRenderData = EntityManager.GetSharedComponentData<EditorRenderData>(e);
                    var materialData = EntityManager.GetSharedComponentData<MaterialProxy>(spriteRenderer.Material);
                    var spriteProxy = EntityManager.GetSharedComponentData<SpriteProxy>(spriteRenderer.Sprite);

                    renderers.Add(new SpriteIntermediateRendererInfo
                    {
                        SpriteID = spriteProxy.Sprite.GetInstanceID(),
                        TextureID = spriteProxy.Sprite.texture.GetInstanceID(),
                        MaterialID = materialData.Material.GetInstanceID(),

                        Color = new UnityEngine.Color(spriteRenderer.Color.r, spriteRenderer.Color.g, spriteRenderer.Color.b, spriteRenderer.Color.a),
                        Transform = localToWorld.Value,
                        Bounds = new UnityEngine.Bounds(localBounds.bounds.Center, localBounds.bounds.Extents),

                        // Removed for the time being. Re-add once we have the BAS working with Sprite data
                        //
                        // VertexData = (System.IntPtr)meshData.Value.Vertices.GetUnsafePtr(),
                        // VertexCount = meshData.Value.Vertices.Length,
                        // IndexData = (System.IntPtr)meshData.Value.Indices.GetUnsafePtr(),
                        // IndexCount = meshData.Value.Indices.Length,

                        SortingLayer = sortLayer.Layer,
                        SortingOrder = sortLayer.Order,

                        SceneCullingMask = editorRenderData.SceneCullingMask,

                        ShaderChannelMask = kSpriteChannelMask
                    });
                }).WithoutBurst().Run();
            }

            SpriteRendererGroup.AddRenderers(renderers.AsArray());
            renderers.Dispose();
            
            return inputDeps;
        }
    }
}
