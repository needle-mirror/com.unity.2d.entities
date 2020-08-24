namespace Unity.U2D.Conversion
{
    internal static class SpriteRendererAuthoringUtils
    {
        public static Unity.U2D.Entities.SpriteMaskInteraction ToU2DSpriteMaskInteraction(this UnityEngine.SpriteMaskInteraction maskInteraction)
        {
            switch (maskInteraction)
            {
                case UnityEngine.SpriteMaskInteraction.None:
                default:
                    return Unity.U2D.Entities.SpriteMaskInteraction.None;
                case UnityEngine.SpriteMaskInteraction.VisibleInsideMask:
                    return Unity.U2D.Entities.SpriteMaskInteraction.VisibleInsideMask;
                case UnityEngine.SpriteMaskInteraction.VisibleOutsideMask:
                    return Unity.U2D.Entities.SpriteMaskInteraction.VisibleOutsideMask;
            }
        }
    }
}