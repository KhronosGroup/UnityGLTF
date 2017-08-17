using Microsoft.VisualStudio.TestTools.UnitTesting;
using GLTFJsonSerialization;
using System.Threading.Tasks;
using System.IO;

namespace GLTFJsonSerializerTests
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
            IGLTFJsonLoader gltfLoader = new GLTFJsonLoader();
            Assert.IsTrue(File.Exists(GLTF_PATH));
            FileStream gltfStream = File.OpenRead(GLTF_PATH);
            GLTFRoot gltfRoot = gltfLoader.Load(gltfStream);
            GLTFJsonLoadTestHelper.TestGLTF(gltfRoot);
        }

        [TestMethod]
        public void LoadGLTFFromByteArray()
        {
            IGLTFJsonLoader gltfLoader = new GLTFJsonLoader();
            Assert.IsTrue(File.Exists(GLTF_PATH));
            FileStream gltfStream = File.OpenRead(GLTF_PATH);

            int streamLength = (int)gltfStream.Length;
            byte[] gltfData = new byte[streamLength];
            gltfStream.Read(gltfData, 0, streamLength);
            GLTFRoot gltfRoot = gltfLoader.Load(gltfData);
            GLTFJsonLoadTestHelper.TestGLTF(gltfRoot);
        }

        [TestMethod]
        public void LoadGLBFromStream()
        {
            IGLTFJsonLoader gltfLoader = new GLTFJsonLoader();
            Assert.IsTrue(File.Exists(GLB_PATH));
            FileStream gltfStream = File.OpenRead(GLB_PATH);
            GLTFRoot gltfRoot = gltfLoader.Load(gltfStream);
            GLTFJsonLoadTestHelper.TestGLB(gltfRoot);
        }
    }
}
