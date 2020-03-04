using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.U2D.Entities;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEditor.U2D;
using DotsRuntimeBuildProfile = Unity.Entities.Runtime.Build.DotsRuntimeBuildProfile;

namespace Unity.U2D.Conversion
{
    internal static class SpriteAsset
    {
        internal static UnityEngine.Texture2D GetSpriteTexture(UnityEngine.Sprite sprite)
        {
            return sprite.IsInAtlas() ? sprite.GetAtlasTexture() : sprite.texture;
        }

        internal static UnityEngine.Rect GetSpriteTextureRect(UnityEngine.Sprite sprite)
        {
            return sprite.IsInAtlas() ? sprite.GetAtlasTextureRect() : sprite.textureRect;
        }

        internal static UnityEngine.Vector2 GetSpriteTextureRectOffset(UnityEngine.Sprite sprite)
        {
            return sprite.IsInAtlas() ? sprite.GetAtlasTextureRectOffset() : sprite.textureRectOffset;
        }
    }

    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    internal class SpriteAssetDeclareAssets : GameObjectConversionSystem
    {
        private bool ShouldPackAtlas()
        {
            return TryGetBuildSettingsComponent<DotsRuntimeBuildProfile>(out var _);
        }

        protected override void OnUpdate()
        {
            if (ShouldPackAtlas())
            {
                SpriteAtlasUtility.PackAllAtlases(UnityEditor.BuildTarget.StandaloneWindows);
            }

            Entities.ForEach((UnityEngine.Sprite sprite) =>
                DeclareReferencedAsset(SpriteAsset.GetSpriteTexture(sprite)));
        }
    }

    [ConverterVersion("2d", 1)]
    internal class SpriteAssetConversion : GameObjectConversionSystem
    {
        private static UnityEngine.Vector4 GetUVTRansform(UnityEngine.Sprite sprite)
        {
            var hasAtlas = sprite.IsInAtlas();
            var so = new UnityEditor.SerializedObject(sprite);
            var atlasRD = hasAtlas ? so.FindProperty("m_AtlasRD") : so.FindProperty("m_RD");
            var uvTrans = atlasRD.FindPropertyRelative("uvTransform").vector4Value;
            return uvTrans;
        }

        private void CreateSpriteAtlas(UnityEngine.Sprite sprite)
        {
            var uvTrans = GetUVTRansform(sprite);
            var texture = SpriteAsset.GetSpriteTexture(sprite);
            var atlas = GetPrimaryEntity(texture);
            
            DstEntityManager.SetName(atlas, "Texture: " + texture.name);
            
            if (!DstEntityManager.HasComponent<SpriteAtlasTexture>(atlas))
            {
                DstEntityManager.AddComponentData(atlas, new SpriteAtlasTexture
                {
                    Texture = atlas,
                });
                DstEntityManager.AddBuffer<SpriteAtlasEntry>(atlas);
            }

            {
                var buffer = DstEntityManager.GetBuffer<SpriteAtlasEntry>(atlas);
                
                var spriteEntity = GetPrimaryEntity(sprite);
                DstEntityManager.SetName(spriteEntity, "Sprite: " + sprite.name);
                
                DstEntityManager.AddComponentData(spriteEntity, new U2D.Entities.Sprite
                {
                    Atlas = atlas,
                    Index = buffer.Length
                });
            }

            using (var allocator = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref allocator.ConstructRoot<SpriteMeshData>();
                var pos = sprite.GetVertexAttribute<UnityEngine.Vector3>(VertexAttribute.Position);

                // Always use GL width here - sprite textures are AlwaysPadded
                float texW = texture.width;
                float texH = texture.height;
                var vertices = allocator.Allocate(ref root.Vertices, pos.Length);
                for(var i = 0; i < pos.Length; i++)
                {
                    var px = pos[i].x;
                    var py = pos[i].y;
                    var ux = (px * uvTrans.x + uvTrans.y) / texW;
                    var uy = (py * uvTrans.z + uvTrans.w) / texH;
                    vertices[i] = new SpriteVertex
                    {
                        Position = pos[i],
                        TexCoord0 = new float2(ux, uy)
                    };
                }

                var indexBuffer = sprite.GetIndices();
                var indices = allocator.Allocate(ref root.Indices, indexBuffer.Length);
                for (var i = 0; i < indexBuffer.Length; i++)
                {
                    indices[i] = indexBuffer[i];
                }

                root.Bounds = new AABB()
                {
                    Center = float3.zero,
                    Extents = new float3(1f)
                };
                
                var entries = DstEntityManager.GetBuffer<SpriteAtlasEntry>(atlas);
                entries.Add(new SpriteAtlasEntry
                {
                    Value = allocator.CreateBlobAssetReference<SpriteMeshData>(Allocator.Temp)
                });
            }
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Sprite sprite) =>
            {
                CreateSpriteAtlas(sprite);
            });
        }        
    }
}
