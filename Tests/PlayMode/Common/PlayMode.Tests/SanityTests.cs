#if !UNITY_DOTSRUNTIME

using System.Collections;
using UnityEngine;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.TestTools;

using Unity.U2D.Entities;

[TestFixture]
public class SanityTests : DotsPlayModeTestFixture
{
	[UnityTest]
 	public IEnumerator SanityTests_FindSpriteAtlasSystem_Exists()
    {
	    yield return MainLoop();

		var system = World.GetExistingSystem<SpriteAtlasSystem>();
		Assert.That(system, Is.Not.Null);
    }
}

#endif //!UNITY_DOTSRUNTIME