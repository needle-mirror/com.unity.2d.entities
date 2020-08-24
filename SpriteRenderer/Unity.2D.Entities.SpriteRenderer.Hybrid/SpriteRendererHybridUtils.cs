using Unity.Entities;

namespace Unity.U2D.Entities
{
    internal static class SpriteRendererHybridUtils
    {
        public static bool IsValidHybridSpriteRenderer(EntityManager entityManager, in SpriteRenderer spriteRenderer)
        {
            if (!entityManager.HasComponent<Material2DProxy>(spriteRenderer.Material))
                return false;
            if (!entityManager.HasComponent<SpriteProxy>(spriteRenderer.Sprite))
                return false;

            return true;
        }        
    }
}