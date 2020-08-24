using Unity.Entities;

namespace Unity.U2D.Entities
{
    internal static class SpriteRendererUtils
    {
        public static bool IsValidSpriteRenderer(in SpriteRenderer spriteRenderer)
        {
            return spriteRenderer.Sprite != Entity.Null && 
                   spriteRenderer.Material != Entity.Null;
        }        
    }
}