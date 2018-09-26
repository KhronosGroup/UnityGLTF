using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using GLTF;
using System.IO;
using GLTF.Schema;

namespace GLTFSerializationTests
{
	[TestClass]
	public class GLTFRootTest
	{
		private readonly string testStr = @"
			{
				""asset"": {
					""version"": ""2.0""
				}
			}
		";

		private GLTFRoot _testRoot;

		[TestInitialize]
		public void Initialize()
		{
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(testStr);
			writer.Flush();
			stream.Position = 0;

			GLTFParser.ParseJson(stream, out _testRoot);

		}

		[TestMethod]
		public void TestMinimumGLTF()
		{
			Assert.AreEqual(_testRoot.Asset.Version, "2.0");
		}

		[TestMethod]
		public void TestCopyMinGLTF()
		{
			GLTFRoot root = new GLTFRoot(_testRoot);
			Assert.IsNotNull(root);
		}
	}
}
