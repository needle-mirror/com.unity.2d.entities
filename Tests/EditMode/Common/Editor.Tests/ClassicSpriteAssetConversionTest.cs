using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

[TestFixture]
public class ClassicSpriteAssetConversionTest : AuthoringTestFixture
{
    [Test]
    public void ConvertSpriteAsset_ConvertDefault_NoSpriteComponent()
    {
        {
            Root = new GameObject();
            CreateClassicComponent<SpriteRenderer>(Root);
        }
        
        Assert.DoesNotThrow(() => { RunConversion(Root); });
        
        using (var spriteQuery = EntityManager.CreateEntityQuery(typeof(Unity.U2D.Entities.Sprite)))
        {
            Assert.That(spriteQuery.CalculateEntityCount(), Is.EqualTo(0));
        }
    }
    
    [Test]
    public void ConvertSpriteAsset_ConvertBasicSprite_SuccessfulConversion()
    {
        var texture = new Texture2D(4, 4);
        var sprite = Sprite.Create(texture, Rect.zero, Vector2.zero);
        
        {
            Root = new GameObject();
            var spriteRenderer = CreateClassicComponent<SpriteRenderer>(Root);
            spriteRenderer.sprite = sprite;
        }
        
        Assert.DoesNotThrow(() => { RunConversion(Root); });
        
        using (var spriteQuery = EntityManager.CreateEntityQuery(typeof(Unity.U2D.Entities.Sprite)))
        {
            Assert.That(spriteQuery.CalculateEntityCount(), Is.EqualTo(1));
        }

        using (var spriteAtlasTextureQuery = EntityManager.CreateEntityQuery(typeof(Unity.U2D.Entities.SpriteAtlasTexture)))
        using (var spriteAtlasEntryQuery = EntityManager.CreateEntityQuery(typeof(Unity.U2D.Entities.SpriteAtlasEntry)))
        {
            Assert.That(spriteAtlasTextureQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(spriteAtlasEntryQuery.CalculateEntityCount(), Is.EqualTo(1));
        }
        
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
    
    [Test]
    public void ConvertSpriteAsset_ConvertSameSpriteTwice_GenerateMeshOnce()
    {
        var spriteQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Unity.U2D.Entities.SpriteAtlasEntry>());
        var texture = new Texture2D(4, 4);
        var sprite = Sprite.Create(texture, Rect.zero, Vector2.zero);
        
        {
            Root = new GameObject();
            var spriteRenderer = CreateClassicComponent<SpriteRenderer>(Root);
            spriteRenderer.sprite = sprite;
        }
        
        Assert.DoesNotThrow(() => { RunConversion(Root); });

        int initialHashCode;
        using (var spriteEntities = spriteQuery.ToEntityArray(Allocator.TempJob))
        {
            var entityIndex = 0;
            var atlasEntries = EntityManager.GetBuffer<Unity.U2D.Entities.SpriteAtlasEntry>(spriteEntities[entityIndex]);
            initialHashCode = atlasEntries[0].Value.GetHashCode();   
        }
        
        Assert.DoesNotThrow(() => { RunConversion(Root); });
        
        using (var spriteEntities = spriteQuery.ToEntityArray(Allocator.TempJob))
        {
            // The conversion world does not keep track of previously converted GOs, but instead creates a new entity for the same GO
            // every time we run the conversion. This is why we have to check for the updated results in index 1 instead of index 0.
            var entityIndex = 1;
            var atlasEntries = EntityManager.GetBuffer<Unity.U2D.Entities.SpriteAtlasEntry>(spriteEntities[entityIndex]);
            var newHashCode = atlasEntries[0].Value.GetHashCode();
            
            Assert.That(newHashCode, Is.EqualTo(initialHashCode));
        }

        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }    
}