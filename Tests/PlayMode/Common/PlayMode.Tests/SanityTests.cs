#if !UNITY_DOTSRUNTIME

using UnityEngine;
using NUnit.Framework;

using Unity.U2D.Entities;

public class SanityTests : DotsPlayModeTestFixture
{
	[Test]
 	public void SanityTests_FindSpriteAtlasSystem_Exists()
    {
	    MainLoop();

		var system = World.GetExistingSystem<SpriteAtlasSystem>();
		Assert.That(system, Is.Not.Null);
    }
}

#endif //!UNITY_DOTSRUNTIME