using System.IO;
using System.Text;
using GLTF;
using NUnit.Framework;

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

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(testStr));
		GLTFParser.ParseJson(stream, out var testRoot);

		Assert.AreEqual(testRoot.Asset.Version, "2.0");
	}
}
