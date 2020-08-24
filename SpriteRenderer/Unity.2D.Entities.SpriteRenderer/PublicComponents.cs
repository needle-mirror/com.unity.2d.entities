using Unity.Entities;
using Unity.Mathematics;

namespace Unity.U2D.Entities
{
    /// <summary>
    /// Component that holds Sprite Renderer data.
    /// </summary> 
    public struct SpriteRenderer : IComponentData
    {
        /// <summary>
        /// Link to the entity holding Sprite data.
        /// </summary>
        /// <value>-</value>> 
        public Entity Sprite;        
        /// <summary>
        /// Link to the entity holding Material data.
        /// </summary>
        /// <value>-</value>>
        public Entity Material;
        /// <summary>
        /// Rendering color for the Sprite graphic. 
        /// </summary>
        /// <value>The default color is white.</value>> 
        public float4 Color;
        /// <summary>
        /// Specifies how the Sprite interacts with the Sprite Masks.
        /// </summary>
        /// <value>-</value>> 
        public SpriteMaskInteraction MaskInteraction;
    }
    
    /// <summary>
    /// Enum that controls the mode under which the Sprite will interact with Sprite Masks.
    /// </summary>
    public enum SpriteMaskInteraction
    {
        /// <summary>
        /// The Sprite will not interact with the masking system.
        /// </summary>
        None,
        /// <summary>
        /// The Sprite will be visible only in areas where a mask is present.
        /// </summary>
        VisibleInsideMask,
        /// <summary>
        /// The Sprite will be visible only in areas where no mask is present.
        /// </summary>
        VisibleOutsideMask
    }    
}