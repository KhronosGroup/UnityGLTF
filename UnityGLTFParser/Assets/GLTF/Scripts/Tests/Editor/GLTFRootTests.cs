using NUnit.Framework;


// todo blgross port to base layer
#if false
public class GLTFRootTest {

	[Test]
	public void TestMinimumGLTF()
	{
		var testStr = @"
			{
				""asset"": {
					""version"": ""2.0""
				}
			}
		";

		var testRoot = GLTFParser.ParseString(testStr);

		Assert.AreEqual(testRoot.Asset.Version, "2.0");
	}
}
#endif