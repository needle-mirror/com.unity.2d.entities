using NUnit.Framework;
using Unity.Collections;

[SetUpFixture]
public class NUnitAssemblyWideSetupEntitiesTests
{
    private NativeLeakDetectionMode OldMode;

    [OneTimeSetUp]
    public void Setup()
    {
        OldMode = NativeLeakDetection.Mode;
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace; // Should have stack trace with tests
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        NativeLeakDetection.Mode = OldMode;
    }
}
