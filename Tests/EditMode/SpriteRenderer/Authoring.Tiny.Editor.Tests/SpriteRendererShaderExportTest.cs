#if UNITY_TINY

using Unity.Entities;
using NUnit.Framework;
using Unity.Build;
using Unity.Build.DotsRuntime;
using Unity.Collections;
using Unity.U2D.Entities;
using Unity.U2D.Conversion;
using Unity.Entities.Runtime.Build;
using Unity.Tiny.Rendering;

using UnityEngine;

using Hash128 = Unity.Entities.Hash128;

[TestFixture]
public class SpriteDefaultShaderExportTest
{
    private World m_World;
    private ComponentSystemBase m_ExportShaderSystem;
    private EntityQuery m_ShaderQuery;
    
    [SetUp]
    public void Setup()
    {
        m_World = new World("SpriteShader_World");
        SetupWorldWithShaderExport();
        
        m_ShaderQuery = m_World.EntityManager.CreateEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<PrecompiledShader>(),
                ComponentType.ReadOnly<VertexShaderBinData>(),
                ComponentType.ReadOnly<FragmentShaderBinData>(),
            }
        });
    }

    private void SetupWorldWithShaderExport()
    {
        var buildConfiguration = new BuildConfiguration();
        buildConfiguration.SetComponent(new DotsRuntimeBuildProfile());

        var configSystemGroup = m_World.GetOrCreateSystem<ConfigurationSystemGroup>();
        var systems = UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(ConfigurationSystemBase));
        Assert.That(systems, Contains.Item(typeof(ExportCommonSpriteShaders)));
        
        foreach (var type in systems)
        {
            var baseSys = (ConfigurationSystemBase)m_World.GetOrCreateSystem(type);
            baseSys.BuildConfiguration = buildConfiguration;
            configSystemGroup.AddSystemToUpdateList(baseSys);
        }
        configSystemGroup.SortSystems();
        
        m_ExportShaderSystem = m_World.GetExistingSystem(typeof(ExportCommonSpriteShaders));
    }

    private VertexShaderBinData GetShaderVertexData(Hash128 shaderGuid)
    {
        using(var compiledShaders = m_ShaderQuery.ToComponentDataArray<PrecompiledShader>(Allocator.TempJob))
        using(var vertexData = m_ShaderQuery.ToComponentDataArray<VertexShaderBinData>(Allocator.TempJob))
        {
            for(var i = 0; i < compiledShaders.Length; i++)
            {
                if (compiledShaders[i].Guid != shaderGuid)
                    continue;

                return vertexData[i];
            }
        }
        return default;
    }
    
    private FragmentShaderBinData GetShaderFragmentData(Hash128 shaderGuid)
    {
        using(var compiledShaders = m_ShaderQuery.ToComponentDataArray<PrecompiledShader>(Allocator.TempJob))
        using(var fragmentData = m_ShaderQuery.ToComponentDataArray<FragmentShaderBinData>(Allocator.TempJob))
        {
            for(var i = 0; i < compiledShaders.Length; i++)
            {
                if (compiledShaders[i].Guid != shaderGuid)
                    continue;

                return fragmentData[i];
            }
        }
        return default;
    }

    [Test]
    public void SpriteDefaultShaderExportTest_RunExport_SpriteShaderExported()
    {
        m_ExportShaderSystem.Update();

        var shaderCount = m_ShaderQuery.CalculateEntityCount();
        Assert.That(shaderCount, Is.GreaterThan(0));

        var noOfSpriteDefaultShaders = 0;
        using(var compiledShaders = m_ShaderQuery.ToComponentDataArray<PrecompiledShader>(Allocator.TempJob))
        {
            for(var i = 0; i < compiledShaders.Length; i++)
            {
                if (compiledShaders[i].Guid == SpriteDefaultShader.Guid)
                    noOfSpriteDefaultShaders++;
            }
            
            Assert.That(noOfSpriteDefaultShaders, Is.EqualTo(1));
        }
    }
    
    [Test]
    public void SpriteDefaultShaderExportTest_RunExport_BinaryDataExported()
    {
        m_ExportShaderSystem.Update();

        var binaryVertexData = GetShaderVertexData(SpriteDefaultShader.Guid);
        Assert.That(binaryVertexData.data.IsCreated, Is.True, $"Binary vertex data has not been created for Sprite Default Shader");
        
        var binaryFragmentData = GetShaderFragmentData(SpriteDefaultShader.Guid);
        Assert.That(binaryFragmentData.data.IsCreated, Is.True, $"Binary fragment data has not been created for Sprite Default Shader");
    }      
    
    [Test]
    public void SpriteDefaultShaderExportTest_RunExport_Dx9Exported()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            Assert.Pass("DX9 Shaders are only exported to windows builds");
            return;
        }

        m_ExportShaderSystem.Update();
        
        var binaryVertexData = GetShaderVertexData(SpriteDefaultShader.Guid);
        Assert.That(binaryVertexData.data.Value.dx9.Length, Is.GreaterThan(0), $"Binary vertex data for DX9 has not been created for Sprite Default Shader");
        
        var binaryFragmentData = GetShaderFragmentData(SpriteDefaultShader.Guid);
        Assert.That(binaryFragmentData.data.Value.dx9.Length, Is.GreaterThan(0), $"Binary fragment data for DX9 has not been created for Sprite Default Shader");
    }
    
    [Test]
    public void SpriteDefaultShaderExportTest_RunExport_Dx11Exported()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            Assert.Pass("DX11 Shaders are only exported to windows builds");
            return;
        }

        m_ExportShaderSystem.Update();
        
        var binaryVertexData = GetShaderVertexData(SpriteDefaultShader.Guid);
        Assert.That(binaryVertexData.data.Value.dx11.Length, Is.GreaterThan(0), $"Binary vertex data for DX11 has not been created for Sprite Default Shader");
        
        var binaryFragmentData = GetShaderFragmentData(SpriteDefaultShader.Guid);
        Assert.That(binaryFragmentData.data.Value.dx11.Length, Is.GreaterThan(0), $"Binary fragment data for DX11 has not been created for Sprite Default Shader");
    }   
    
    [Test]
    public void SpriteDefaultShaderExportTest_RunExport_OpenGLExported()
    {
        m_ExportShaderSystem.Update();
        
        var binaryVertexData = GetShaderVertexData(SpriteDefaultShader.Guid);
        Assert.That(binaryVertexData.data.Value.glsl.Length, Is.GreaterThan(0), $"Binary vertex data for OpenGL has not been created for Sprite Default Shader");
        
        var binaryFragmentData = GetShaderFragmentData(SpriteDefaultShader.Guid);
        Assert.That(binaryFragmentData.data.Value.glsl.Length, Is.GreaterThan(0), $"Binary fragment data for OpenGL has not been created for Sprite Default Shader");
    }   
    
    [Test]
    public void SpriteDefaultShaderExportTest_RunExport_MetalExported()
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
        {
            Assert.Pass("Metal Shaders are only exported to mac builds");
            return;
        }        
        
        m_ExportShaderSystem.Update();
        
        var binaryVertexData = GetShaderVertexData(SpriteDefaultShader.Guid);
        Assert.That(binaryVertexData.data.Value.metal.Length, Is.GreaterThan(0), $"Binary vertex data for Metal has not been created for Sprite Default Shader");
        
        var binaryFragmentData = GetShaderFragmentData(SpriteDefaultShader.Guid);
        Assert.That(binaryFragmentData.data.Value.metal.Length, Is.GreaterThan(0), $"Binary fragment data for Metal has not been created for Sprite Default Shader");
    }     
}

#endif //UNITY_TINY