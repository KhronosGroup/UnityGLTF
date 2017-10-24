using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.IO;
using GLTF;
using GLTF.Schema;

namespace GLTFSerializationTests
{
	[TestClass]
	public class GLTFJsonLoaderTest
	{
		private readonly string GLTF_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
		private readonly string GLB_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/BoomBox.glb";

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void LoadGLTFFromStream()
		{
			Assert.IsTrue(File.Exists(GLTF_PATH));
			FileStream gltfStream = File.OpenRead(GLTF_PATH);

			GLTFRoot.RegisterExtension(new TestExtensionFactory());
			GLTFRoot gltfRoot = GLTFParser.ParseJson(gltfStream);
			GLTFJsonLoadTestHelper.TestGLTF(gltfRoot);
		}

		[TestMethod]
		public void LoadGLBFromStream()
		{
			Assert.IsTrue(File.Exists(GLB_PATH));
			FileStream gltfStream = File.OpenRead(GLB_PATH);
			GLTFRoot gltfRoot = GLTFParser.ParseJson(gltfStream);
			GLTFJsonLoadTestHelper.TestGLB(gltfRoot);
		}
	}
}
