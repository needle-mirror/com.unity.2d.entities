using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

using Camera = Unity.Tiny.Rendering.Camera;

namespace Unity.U2D.Entities
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal class EmitDrawCallBarrier : EntityCommandBufferSystem
    {
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EmitDrawCallBarrier))]
    internal class CombineDrawCallSystem : JobComponentSystem
    {
        private DrawInstructionComparer m_DrawInstructionComparer;
        
        protected override void OnCreate()
        {
            m_DrawInstructionComparer = new DrawInstructionComparer(); 
        }        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
#if UNITY_EDITOR
            // Added since the systems might run while the editor is compiling
            if (UnityEditor.EditorApplication.isCompiling)
            {
                return default; 
            }
#endif            
            
            var drawInstructions = GetBufferFromEntity<DrawInstruction>();
            unsafe
            {
                inputDeps = Entities
                    .WithNativeDisableParallelForRestriction(drawInstructions)
                    .ForEach((Entity e, 
                        ref DrawCall dc, 
                        ref SpriteMeshReference mrd) =>
                    {
                        var data = drawInstructions[dc.TargetCamera];
                        data.Add(new DrawInstruction
                        {
                            Texture = dc.Texture,
                            Material = dc.Material,
                            VertexData = (System.IntPtr)mrd.Value.Value.Vertices.GetUnsafePtr(),
                            VertexCount = mrd.Value.Value.Vertices.Length,
                            IndexData = (System.IntPtr)mrd.Value.Value.Indices.GetUnsafePtr(),
                            IndexCount = mrd.Value.Value.Indices.Length,
                            Color = dc.Color,
                            Transform = dc.Transform,
                            LocalBounds = dc.LocalBounds,
                            DrawOrder = dc.DrawOrder,
                            SortingLayer = dc.SortingLayer,
                            SortingOrder = dc.SortingOrder
                        });
                    }).Schedule(inputDeps);
            
                inputDeps = Entities
                    .WithNativeDisableParallelForRestriction(drawInstructions)
                    .ForEach((Entity e, 
                        DynamicBuffer<BatchedVertex> vertices, 
                        DynamicBuffer<BatchedIndex> indices, 
                        ref DrawCall dc) =>
                    {
                        var data = drawInstructions[dc.TargetCamera];
                        data.Add(new DrawInstruction
                        {
                            Texture = dc.Texture,
                            Material = dc.Material,
                            VertexData = (System.IntPtr)vertices.GetUnsafePtr(),
                            VertexCount = vertices.Length,
                            IndexData = (System.IntPtr)indices.GetUnsafePtr(),
                            IndexCount = indices.Length,
                            Color = dc.Color,
                            Transform = dc.Transform,
                            LocalBounds = dc.LocalBounds,
                            DrawOrder = dc.DrawOrder,
                            SortingLayer = dc.SortingLayer,
                            SortingOrder = dc.SortingOrder
                        });
                    }).Schedule(inputDeps);
            }

            inputDeps.Complete();
            Entities
                .WithAll<Camera>()
                .WithNativeDisableParallelForRestriction(drawInstructions)
                .WithoutBurst()
                .ForEach((Entity e) =>
                {
                    var instructionsArray = drawInstructions[e].AsNativeArray();
                    instructionsArray.Sort(m_DrawInstructionComparer);
                }).Run();

            return inputDeps;
        }
    }
}