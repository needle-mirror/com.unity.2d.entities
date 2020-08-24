using Unity.Entities;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    /// <summary>
    /// Data structure that represents a vertex in a Sprite mesh.
    /// </summary>
    public struct SpriteVertex
    {
        /// <summary>
        /// Position of the vertex.
        /// </summary>
        /// <value>-</value>> 
        public float3 Position;
        /// <summary>
        /// UV coordinate of the vertex.
        /// </summary>
        /// <value>-</value>>
        public float2 TexCoord0;
    }
    
    /// <summary>
    /// Data structure that represents a Sprite's mesh.
    /// </summary>
    public struct SpriteMesh
    {
        /// <summary>
        /// Array holding all vertex data of the mesh.
        /// </summary>
        /// <value>-</value>> 
        public BlobArray<SpriteVertex> Vertices;
        /// <summary>
        /// Array holding all index data of the mesh.
        /// </summary>
        /// <value>-</value>> 
        public BlobArray<ushort> Indices;
        /// <summary>
        /// Mesh bounds.
        /// </summary>
        /// <value>-</value>> 
        public AABB Bounds;
    }

    /// <summary>
    /// Component that holds Sprite render data.
    /// </summary>      
    public struct SpriteRenderData : IComponentData
    {
        /// <summary>
        /// Reference to the Sprite's mesh.
        /// </summary>
        /// <value>-</value>> 
        public BlobAssetReference<SpriteMesh> Mesh;
        /// <summary>
        /// Link to the entity holding Texture data.
        /// </summary>
        /// <value>-</value>>
        public Entity Texture;
    }

    /// <summary>
    /// Component that holds a link to the entity holding the atlas' Texture data.
    /// </summary>   
    public struct SpriteAtlasTexture : IComponentData
    {
        /// <summary>
        /// Link to the entity holding Texture data.
        /// </summary>
        /// <value>-</value>> 
        public Entity Texture;
    }

    /// <summary>
    /// BufferElement that holds Sprite render data.
    /// </summary> 
    public struct SpriteAtlasEntry : IBufferElementData
    {
        /// <summary>
        /// Reference to the Sprite's mesh.
        /// </summary>
        /// <value>-</value>> 
        public BlobAssetReference<SpriteMesh> Value;
    }

    /// <summary>
    /// Component that holds Sprite data.
    /// </summary> 
    public struct Sprite : IComponentData
    {
        /// <summary>
        /// The Sprite's index in the atlas' array of Sprites.
        /// </summary>
        /// <value>-</value>> 
        public int Index;
        /// <summary>
        /// Link to the entity holding atlas data.
        /// </summary>
        /// <value>-</value>> 
        public Entity Atlas;
    }

    /// <summary>
    /// Component that holds general 2D rendering data.
    /// </summary> 
    public struct Renderer2D : IComponentData
    {
        /// <summary>
        ///  The Sorting Layer that the Renderer is set to, which determines its priority in the render queue. 
        /// </summary>
        /// <value>
        /// Lower valued Renderers are rendered first, with higher valued Renderers overlapping them and appearing closer to the camera.
        /// </value>
        public short SortingLayer;
        /// <summary>
        ///  The Order in Layer value of the Renderer, which determines its render priority within its Sorting Layer. 
        /// </summary>
        /// <value>
        /// Lower valued Renderers are rendered first, with higher valued Renderers overlapping them and appearing closer to the camera.
        /// </value>
        public short OrderInLayer;
        /// <summary>
        /// Local bounds of the Renderer.
        /// </summary>
        /// <value>-</value>> 
        public AABB Bounds;
    }
}
