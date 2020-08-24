using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.U2D.Entities
{
    internal static class TestUtilities
    {
        public const float Epsilon = 0.001f;
        
        private static BlobAssetReference<SpriteMesh> kQuadMesh = CreateQuad();
        
        public static BlobAssetReference<SpriteMesh> CreateQuad(float width = 1f, float height = 1f)
        {
            BlobAssetReference<SpriteMesh> quad;
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<SpriteMesh>();

                var vertices = builder.Allocate(ref root.Vertices, 4);
                vertices[0] = new SpriteVertex()
                {
                    Position = new float3(0f, 0f, 0f),
                    TexCoord0 = new float2(0, 0)
                };
                vertices[1] = new SpriteVertex()
                {
                    Position = new float3(width, 0f, 0f),
                    TexCoord0 = new float2(1, 0)
                };
                vertices[2] = new SpriteVertex()
                {
                    Position = new float3(0f, height, 0f),
                    TexCoord0 = new float2(0, 1)
                };
                vertices[3] = new SpriteVertex()
                {
                    Position = new float3(width, height, 0f),
                    TexCoord0 = new float2(1, 1)
                };

                var indices = builder.Allocate(ref root.Indices, 6);
                indices[0] = 0;
                indices[1] = 2;
                indices[2] = 1;
                indices[3] = 2;
                indices[4] = 3;
                indices[5] = 1;

                quad = builder.CreateBlobAssetReference<SpriteMesh>(Allocator.Persistent);
            }
            
            return quad;
        }  
        
        public static Entity CreateDummySprite(EntityManager manager)
        {
            var spriteEntity = manager.CreateEntity(typeof(SpriteRenderData));
            manager.SetComponentData(spriteEntity, new SpriteRenderData()
            {
                Mesh = kQuadMesh,
                Texture = Entity.Null
            });

            return spriteEntity;
        }
        
        public static Entity CreateDummyMaterial(EntityManager manager)
        {
            var materialEntity = manager.CreateEntity();
            return materialEntity;
        }
        
        public static Entity CreateDummyRenderer2D(EntityManager manager, float3 position)
        {
            var rendererEntity = manager.CreateEntity(typeof(LocalToWorld));

            manager.AddComponentData(rendererEntity, new Translation() { Value = position });
            manager.AddComponentData(rendererEntity, new Rotation() { Value = quaternion.identity });
            manager.AddComponentData(rendererEntity, new Renderer2D()
            {
                SortingLayer = 0,
                OrderInLayer = 0,
                Bounds = new AABB()
                {
                    Center = new float3(0f),
                    Extents = new float3(1f)
                }             
            });            
            
            return rendererEntity;
        }        
    }
}
