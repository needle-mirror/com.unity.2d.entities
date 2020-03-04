using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Tiny.Rendering;

using Color = Unity.Tiny.Color;
using Colors = Unity.Tiny.Colors;

namespace Unity.U2D.Entities
{
    internal struct DrawCall : IComponentData
    {
        public Entity Texture;
        public Entity Material;

        public Color Color;
        
        public float4x4 Transform;
        public AABB LocalBounds;

        public int DrawOrder;
        public int SortingLayer;
        public int SortingOrder;

        public Entity TargetCamera;
    }

    internal struct SpriteMeshReference: IComponentData
    {
        public BlobAssetReference<SpriteMeshData> Value;
    }

    internal struct BatchedVertex : IBufferElementData
    {
        public float3 Position;
        public float2 TexCoord0;
    }

    internal struct BatchedIndex : IBufferElementData
    {
        public ushort Value;
    }
    
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(SpriteSortingSystem))]
    [UpdateBefore(typeof(EmitDrawCallBarrier))]
    internal class SpriteBatchingSystem : JobComponentSystem
    {
        private struct BatchSprites
        {
            [ReadOnly] public int JobIndex;
            [ReadOnly] public int MaxVertexCountPerBatch; 
            [ReadOnly] public int MaxIndexCountPerBatch;
            [ReadOnly] public BufferFromEntity<RenderItem> RenderItemBuffers;
            [ReadOnly] public ComponentDataFromEntity<SpriteRenderer> SpriteRenderers;
            [ReadOnly] public ComponentDataFromEntity<ObjectBounds> ObjectBounds;
            [ReadOnly] public ComponentDataFromEntity<SortLayer> SortData;
            [ReadOnly] public ComponentDataFromEntity<SpriteRenderData> SpriteRenderDatas;

            public EntityCommandBuffer.Concurrent Commands;

            private int m_BatchCounter;
            
            private void MakeOneBatch(Entity camera, DynamicBuffer<RenderItem> renderItems, int startIndex, int count, Entity material, Entity texture, Color currentTint, int vertexCount, int indexCount)
            {
                var e = Commands.CreateEntity(JobIndex);
                
                if (count == 1)
                {
                    var item = renderItems[startIndex];
                    var spriteRenderer = SpriteRenderers[item.Renderer];
                    var localBounds = ObjectBounds[item.Renderer];
                    var sortData = SortData[item.Renderer];
                    var srd = SpriteRenderDatas[spriteRenderer.Sprite];

                    Commands.AddComponent(JobIndex, e, new DrawCall
                    {
                        Texture = texture,
                        Material = material,
                        Color = currentTint,
                        Transform = item.Transform,
                        LocalBounds = localBounds.bounds,
                        DrawOrder = m_BatchCounter,
                        SortingLayer = sortData.Layer,
                        SortingOrder = sortData.Order,
                        TargetCamera = camera
                    });
                    
                    Commands.AddComponent(JobIndex, e, new SpriteMeshReference
                    {
                        Value = srd.Mesh
                    });
                    
                }
                else
                {
                    // TODO this could be very slow to upload every frame. Consider writing to gfx memory.
                    var dstVertices = Commands.AddBuffer<BatchedVertex>(JobIndex, e);
                    dstVertices.ResizeUninitialized(vertexCount);
                    var dstIndices = Commands.AddBuffer<BatchedIndex>(JobIndex, e);
                    dstIndices.ResizeUninitialized(indexCount);
    
                    var minMaxAABB = MinMaxAABB.Empty;
                    var vertOffset = 0;
                    var idxOffset = 0;
                    
                    for (var i = startIndex; i < (startIndex + count); i++)
                    {
                        var item = renderItems[i];
                        var spriteRenderer = SpriteRenderers[item.Renderer];
                        var srd = SpriteRenderDatas[spriteRenderer.Sprite];
                        ref var sprite = ref srd.Mesh.Value;
                        
                        for (var k = 0; k < sprite.Vertices.Length; k++)
                        {
                            dstVertices[k + vertOffset] = new BatchedVertex 
                            {
                                Position = math.transform(item.Transform, sprite.Vertices[k].Position),   
                                TexCoord0 = sprite.Vertices[k].TexCoord0
                            };
                        }

                        for (var k = 0; k < sprite.Indices.Length; k++)
                        {
                            dstIndices[idxOffset + k] = new BatchedIndex {Value = (ushort)(sprite.Indices[k] + vertOffset)};
                        }
                        
                        vertOffset += sprite.Vertices.Length;
                        idxOffset += sprite.Indices.Length;

                        var localBounds = ObjectBounds[item.Renderer];
                        var transformedBounds = AABB.Transform(item.Transform, localBounds.bounds);
                        minMaxAABB.Encapsulate(transformedBounds);
                    }

                    var startItem = renderItems[startIndex];
                    var sortData = SortData[startItem.Renderer];
                    Commands.AddComponent(JobIndex, e, new DrawCall 
                    {
                        Texture = texture,
                        Material = material,
                        Color = currentTint,
                        Transform = float4x4.identity, // TODO if the offsets are huge, there maybe floating issues
                        LocalBounds = minMaxAABB,     // this should be world bounds of the batched items
                        DrawOrder = m_BatchCounter,
                        SortingLayer = sortData.Layer,
                        SortingOrder = sortData.Order,
                        TargetCamera = camera
                    });
                }

                m_BatchCounter++;
            }
            
            private int Batch(Entity camera, DynamicBuffer<RenderItem> renderItems, int startIndex)
            {
                var vertexCount = 0;
                var indexCount = 0;
                var currentTexture = Entity.Null;
                var currentMaterial = Entity.Null;
                var currentTint = Colors.Transparent;
                
                var startItem = renderItems[startIndex];
                
                if (startItem.Type == RenderItemType.Sprite)
                {
                    var spriteRenderer = SpriteRenderers[startItem.Renderer];
                    var srd = SpriteRenderDatas[spriteRenderer.Sprite];
                    currentTexture = srd.Texture;
                    currentMaterial = spriteRenderer.Material;
                    currentTint = spriteRenderer.Color;
                }

                var batchSize = 0;
                for (var i = startIndex; i < renderItems.Length; i++)
                {
                    
                    var item = renderItems[i];
                    var texture = Entity.Null;
                    var material = Entity.Null;
                    var tintColor = Colors.Transparent;

                    if (item.Type == RenderItemType.Sprite)
                    {
                        var spriteRenderer = SpriteRenderers[item.Renderer];
                        var srd = SpriteRenderDatas[spriteRenderer.Sprite];
                        texture = srd.Texture;
                        material = spriteRenderer.Material;
                        tintColor = spriteRenderer.Color;
                    }
                    
                    if(currentTexture != texture ||
                       currentMaterial != material ||
                       vertexCount > MaxVertexCountPerBatch ||
                       indexCount > MaxIndexCountPerBatch ||
                       item.Type != RenderItemType.Sprite ||
                       !currentTint.Value.Equals(tintColor.Value))
                   {
                       // create a batch
                       MakeOneBatch(camera, renderItems, startIndex, batchSize, currentMaterial, currentTexture, currentTint, vertexCount, indexCount);
                       return startIndex + batchSize;
                   }

                    if (item.Type == RenderItemType.Sprite)
                    {
                        var spriteRenderer = SpriteRenderers[item.Renderer];
                        var srd = SpriteRenderDatas[spriteRenderer.Sprite];
                        vertexCount += srd.Mesh.Value.Vertices.Length;
                        indexCount += srd.Mesh.Value.Indices.Length;
                        batchSize++;
                    }
                }
                
                
                var leftOver = renderItems.Length - startIndex;
                if(leftOver > 0)
                    MakeOneBatch(camera, renderItems, startIndex, leftOver, currentMaterial, currentTexture, currentTint, vertexCount, indexCount);

                return renderItems.Length;
            }
            
            public void Execute(Entity entity, [ReadOnly] ref Camera camera)
            {
                m_BatchCounter = 0;
                
                var renderItems = RenderItemBuffers[entity];
                for (var i = 0; i < renderItems.Length;)
                {
                    i = Batch(entity, renderItems, i);
                } 
            }
        }        

        private EmitDrawCallBarrier m_Barrier;
        
        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<EmitDrawCallBarrier>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var renderItemBuffers = GetBufferFromEntity<RenderItem>(true);
            var spriteRenderers = GetComponentDataFromEntity<SpriteRenderer>(true);
            var objectBounds = GetComponentDataFromEntity<ObjectBounds>(true);
            var sortData = GetComponentDataFromEntity<SortLayer>(true);
            var spriteRenderData = GetComponentDataFromEntity<SpriteRenderData>(true);
            var cmdBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent();

            var maxVertexCountPerBatch = Renderer2DSettings.MaxVertexCountPerBatch;
            var maxIndexCountPerBatch = Renderer2DSettings.MaxIndexCountPerBatch;

            inputDeps = Entities
                .WithReadOnly(renderItemBuffers)
                .WithReadOnly(spriteRenderers)
                .WithReadOnly(objectBounds)
                .WithReadOnly(sortData)
                .WithReadOnly(spriteRenderData)
                .ForEach((Entity entity, int entityInQueryIndex, ref Camera camera) =>
                {
                    new BatchSprites()
                    {
                        JobIndex = entityInQueryIndex,
                        MaxVertexCountPerBatch = maxVertexCountPerBatch,
                        MaxIndexCountPerBatch = maxIndexCountPerBatch,
                        RenderItemBuffers = renderItemBuffers,
                        SpriteRenderers = spriteRenderers,
                        ObjectBounds = objectBounds,
                        SortData = sortData,
                        SpriteRenderDatas = spriteRenderData,
                        Commands = cmdBuffer
                    }.Execute(entity, ref camera);
                }).Schedule(inputDeps);
            
            m_Barrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }
    }
}