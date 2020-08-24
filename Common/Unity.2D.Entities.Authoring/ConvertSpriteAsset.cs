using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.U2D.Entities;

using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEditor.U2D;

using Hash128 = Unity.Entities.Hash128;

namespace Unity.U2D.Conversion
{
    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
    internal class SpriteProxyConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Sprite sprite) =>
            {
                var proxyEntity = GetPrimaryEntity(sprite);
                DstEntityManager.AddSharedComponentData(proxyEntity, new SpriteProxy
                {
                    Sprite = sprite
                });                
            });
        }
    }    
    
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
    [WorldSystemFilter(WorldSystemFilterFlags.DotsRuntimeGameObjectConversion)]
    internal class PackAtlasSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            SpriteAtlasUtility.PackAllAtlases(UnityEditor.BuildTarget.StandaloneWindows);
        }
    }
    
    [ConverterVersion("2d", 1)]
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    internal class SpriteAssetDeclareTexture : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            SpriteAtlasUtility.PackAllAtlases(UnityEditor.BuildTarget.StandaloneWindows);

            Entities.ForEach((UnityEngine.Sprite sprite) =>
                DeclareReferencedAsset(SpriteAsset.GetSpriteTexture(sprite)));
        }
    }    
    
    [BurstCompile]
    internal struct CreateSpriteBlobJob : IJob
    {
        [ReadOnly] public SpriteAssetConversion.ConversionInfo conversionInfo;
        [ReadOnly] public float4 UVTransform;
        [ReadOnly] public float2 TextureSize;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<ushort> Indices;
        
        [NativeDisableContainerSafetyRestriction] public NativeArray<BlobAssetReference<SpriteMesh>> SpriteBlobs;
        
        public void Execute()
        {
            var allocator = new BlobBuilder(Allocator.Temp);
            ref var root = ref allocator.ConstructRoot<SpriteMesh>();

            // Always use GL width here - sprite textures are AlwaysPadded
            var texW = TextureSize.x;
            var texH = TextureSize.y;

            var vertices = allocator.Allocate(ref root.Vertices, Positions.Length);
            var minSize = new float2(float.MaxValue); 
            var maxSize = new float2(float.MinValue);
            for(var i = 0; i < Positions.Length; i++)
            {
                var px = Positions[i].x;
                var py = Positions[i].y;
                var ux = (px * UVTransform.x + UVTransform.y) / texW;
                var uy = (py * UVTransform.z + UVTransform.w) / texH;
                vertices[i] = new SpriteVertex
                {
                    Position = Positions[i],
                    TexCoord0 = new float2(ux, uy)
                };

                minSize.x = math.min(minSize.x, px);
                minSize.y = math.min(minSize.y, py);
                maxSize.x = math.max(maxSize.x, px);
                maxSize.y = math.max(maxSize.y, py);
            }
            
            var resultIndices = allocator.Allocate(ref root.Indices, Indices.Length);
            for (var i = 0; i < Indices.Length; i++)
            {
                resultIndices[i] = Indices[i];
            }

            root.Bounds = new AABB()
            {
                Center = float3.zero,
                Extents = new float3((maxSize.x - minSize.x) / 2f, (maxSize.y - minSize.y) / 2f, 1f)
            };
            
            SpriteBlobs[conversionInfo.BlobIndex] = allocator.CreateBlobAssetReference<SpriteMesh>(Allocator.Persistent);
            allocator.Dispose();
        }
    }

    [ConverterVersion("2d", 2)]
    internal class SpriteAssetConversion : GameObjectConversionSystem
    {
        internal struct ConversionInfo
        {
            public Hash128 Hash;
            public int BlobIndex;
        }        
        
        private EntityQuery m_SpriteQuery;

        private static Hash128 GetSpriteHash(UnityEngine.Sprite uSprite)
        {
            var spriteGuidHash = ConvertUtils.GetObjectGuidHash(uSprite);

            var pathToMetaFile = ConvertUtils.GetFullAssetMetaPath(uSprite);
            var lastModifiedHash = !string.IsNullOrEmpty(pathToMetaFile) ? ConvertUtils.GetFileLastModifiedHash(pathToMetaFile) : new Hash128(0, 0, 0, 0);

            spriteGuidHash.Value += lastModifiedHash.Value;
            return spriteGuidHash;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SpriteQuery = GetEntityQuery(ComponentType.ReadOnly<UnityEngine.Sprite>());
        }

        protected override void OnUpdate()
        {
            var noOfSprites = m_SpriteQuery.CalculateEntityCount();
            
            var spriteMeshContext = new BlobAssetComputationContext<ConversionInfo, SpriteMesh>(BlobAssetStore, noOfSprites, Allocator.Temp);
            
            var spriteBlobs = new NativeArray<BlobAssetReference<SpriteMesh>>(noOfSprites, Allocator.TempJob);
            var computeHandle = new JobHandle();
            var blobCounter = 0;
            
            Entities.ForEach((UnityEngine.Sprite uSprite) =>
            {
                var spriteHash = GetSpriteHash(uSprite);
                spriteMeshContext.AssociateBlobAssetWithUnityObject(spriteHash, uSprite);
                
                if (spriteMeshContext.NeedToComputeBlobAsset(spriteHash))
                {
                    var texture = SpriteAsset.GetSpriteTexture(uSprite);

                    var positionSlice = uSprite.GetVertexAttribute<UnityEngine.Vector3>(VertexAttribute.Position);
                    TransformVertexSliceToArray(positionSlice, out var positionArray);

                    var conversionData = new ConversionInfo()
                    {
                        Hash = spriteHash,
                        BlobIndex = blobCounter++,
                    };

                    var blobJob = new CreateSpriteBlobJob()
                    {
                        conversionInfo = conversionData,
                        UVTransform = GetUVTransform(uSprite),
                        TextureSize = new float2(texture.width, texture.height),
                        Positions = positionArray,
                        Indices = uSprite.GetIndices(),                        
                        SpriteBlobs = spriteBlobs
                    };
                    
                    spriteMeshContext.AddBlobAssetToCompute(spriteHash, conversionData);
                    computeHandle = JobHandle.CombineDependencies(computeHandle, blobJob.Schedule());
                }
            });
            
            computeHandle.Complete();
            
            using (var conversionData = spriteMeshContext.GetSettings(Allocator.TempJob))
            {
                for (var i = 0; i < conversionData.Length; i++)
                {
                    spriteMeshContext.AddComputedBlobAsset(conversionData[i].Hash, spriteBlobs[conversionData[i].BlobIndex]);
                }
            }
            
            Entities.ForEach((UnityEngine.Sprite uSprite) =>
            {
                var spriteHash = GetSpriteHash(uSprite);
                spriteMeshContext.GetBlobAsset(spriteHash, out var spriteBlobReference);
                CreateSpriteAtlas(uSprite, spriteBlobReference);
            });

            spriteBlobs.Dispose();
            spriteMeshContext.Dispose();
        }

        private static UnityEngine.Vector4 GetUVTransform(UnityEngine.Sprite sprite)
        {
            var hasAtlas = sprite.IsInAtlas();
            var so = new UnityEditor.SerializedObject(sprite);
            var atlasRD = hasAtlas ? so.FindProperty("m_AtlasRD") : so.FindProperty("m_RD");
            var uvTrans = atlasRD.FindPropertyRelative("uvTransform").vector4Value;
            return uvTrans;
        }

        private static void TransformVertexSliceToArray(in NativeSlice<UnityEngine.Vector3> vertexSlice, out NativeArray<float3> vertexArray)
        {
            vertexArray = new NativeArray<float3>(vertexSlice.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < vertexSlice.Length; i++)
            {
                vertexArray[i] = vertexSlice[i];
            }
        }

        private void CreateSpriteAtlas(UnityEngine.Sprite sprite, BlobAssetReference<SpriteMesh> blobReference)
        {
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

            var buffer = DstEntityManager.GetBuffer<SpriteAtlasEntry>(atlas);
            
            var spriteEntity = GetPrimaryEntity(sprite);
            DstEntityManager.SetName(spriteEntity, "Sprite: " + sprite.name);
            
            DstEntityManager.AddComponentData(spriteEntity, new U2D.Entities.Sprite
            {
                Atlas = atlas,
                Index = buffer.Length
            });

            var entries = DstEntityManager.GetBuffer<SpriteAtlasEntry>(atlas);
            entries.Add(new SpriteAtlasEntry
            {
                Value = blobReference
            });
        }        
    }
}
