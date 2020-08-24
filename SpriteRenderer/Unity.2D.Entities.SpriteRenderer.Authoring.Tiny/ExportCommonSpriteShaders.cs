#if UNITY_TINY

using Unity.U2D.Entities;
using Unity.Entities;
using Unity.Build.DotsRuntime;

using Unity.TinyConversion;

namespace Unity.U2D.Conversion
{
    [DisableAutoCreation]
    internal class ExportCommonSpriteShaders : ShaderExportSystem
    {
        static readonly string k_BinaryShaderFolderPath = "Packages/com.unity.2d.entities/SpriteRenderer/Unity.2D.Entities.SpriteRenderer.Authoring.Tiny/shaderbin~/";

        protected override void OnUpdate()
        {
            if (BuildConfiguration == null)
                return;
            if (!BuildConfiguration.TryGetComponent<DotsRuntimeBuildProfile>(out var profile))
                return;

            var rendererTypes = GetShaderFormat(profile.Target);
            
            CreateShaderDataEntity(k_BinaryShaderFolderPath, SpriteDefaultShader.Guid, "sprite", rendererTypes);
        }
    }
}

#endif //UNITY_TINY