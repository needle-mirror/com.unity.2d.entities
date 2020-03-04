using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Unity.U2D.Entities
{
    public static class SpriteMeshCreator
    {
        public static BlobAssetReference<SpriteMeshData> CreateQuad(float width = 1f, float height = 1f)
        {
            BlobAssetReference<SpriteMeshData> quad;
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<SpriteMeshData>();

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

                quad = builder.CreateBlobAssetReference<SpriteMeshData>(Allocator.Persistent);
            }
            
            return quad;
        }
    }
} 