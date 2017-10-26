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
        readonly string GLTF_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
		readonly string GLB_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/BoomBox.glb";

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void LoadGLTFFromStream()
		{
			Assert.IsTrue(File.Exists(GLTF_PATH));
			FileStream gltfStream = File.OpenRead(GLTF_PATH);
			// todo: this code does not work if file is greater than 4 gb
			int streamLength = (int)gltfStream.Length;
			byte[] gltfData = new byte[streamLength];
			gltfStream.Read(gltfData, 0, streamLength);
			
			GLTFRoot.RegisterExtension(new TestExtensionFactory());
            GLTFRoot gltfRoot = GLTFParser.ParseJson(gltfData);
			GLTFJsonLoadTestHelper.TestGLTF(gltfRoot);
		}

		[TestMethod]
		public void LoadGLBFromStream()
		{
			Assert.IsTrue(File.Exists(GLB_PATH));
			FileStream gltfStream = File.OpenRead(GLB_PATH);

			// todo: this code does not work if file is greater than 4 gb
			int streamLength = (int)gltfStream.Length;
			byte[] gltfData = new byte[streamLength];
			gltfStream.Read(gltfData, 0, streamLength);

			GLTFRoot gltfRoot = GLTFParser.ParseJson(gltfData);
			GLTFJsonLoadTestHelper.TestGLB(gltfRoot);
		}
	}
}
