using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    public static class Renderer2DSettings
    {
        private const int k_DefaultMaxVertexCount = 4096;
        private const int k_DefaultMaxIndexCount = 8192; 
        
        /// <summary>
        /// The maximum amount of vertices that can fit inside one batch...
        /// <para>Note: This setting only applies to builds.</para>
        /// </summary>
        public static int MaxVertexCountPerBatch
        {
            get => m_MaxVertexCountPerBatch;
            set => m_MaxVertexCountPerBatch = math.clamp(value, 1, k_DefaultMaxVertexCount);
        }
        private static int m_MaxVertexCountPerBatch = k_DefaultMaxVertexCount;

        /// <summary>
        /// The maximum amount of indices that can fit inside one batch
        /// <para>Note: This setting only applies to builds.</para>
        /// </summary>
        public static int MaxIndexCountPerBatch
        {
            get => m_MaxIndexCountPerBatch;
            set => m_MaxIndexCountPerBatch = math.clamp(value, 1, k_DefaultMaxIndexCount);
        }
        private static int m_MaxIndexCountPerBatch = k_DefaultMaxIndexCount;
    }
}