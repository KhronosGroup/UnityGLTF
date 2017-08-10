using Microsoft.VisualStudio.TestTools.UnitTesting;
using GLTFSerializer;
using System.Threading.Tasks;
using System.IO;

namespace GLTFSerializerTests
{
    [TestClass]
    public class GLTFLoaderTest
    {
        readonly string GLTF_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
        readonly string GLB_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/BoomBox.glb";

        public TestContext TestContext { get; set; }
        
        [TestMethod]
        public async Task LoadGLTFFromPath()
        {
            IGLTFLoader gltfLoader = new GLTFLoader();
            Assert.IsTrue(File.Exists(GLTF_PATH));
            GLTFRoot gltfRoot = await gltfLoader.Load(GLTF_PATH);
            GLTFLoadTestHelper.TestGLTF(gltfRoot);
        }

        [TestMethod]
        public async Task LoadGLTFFromStream()
        {
            IGLTFLoader gltfLoader = new GLTFLoader();
            Assert.IsTrue(File.Exists(GLTF_PATH));
            FileStream gltfStream = File.OpenRead(GLTF_PATH);
            GLTFRoot gltfRoot = await gltfLoader.Load(gltfStream);
            GLTFLoadTestHelper.TestGLTF(gltfRoot);
        }

        [TestMethod]
        public async Task LoadGLTFFromByteArray()
        {
            IGLTFLoader gltfLoader = new GLTFLoader();
            Assert.IsTrue(File.Exists(GLTF_PATH));
            FileStream gltfStream = File.OpenRead(GLTF_PATH);

            int streamLength = (int)gltfStream.Length;
            byte[] gltfData = new byte[streamLength];
            gltfStream.Read(gltfData, 0, streamLength);
            GLTFRoot gltfRoot = await gltfLoader.Load(gltfData);
            GLTFLoadTestHelper.TestGLTF(gltfRoot);
        }

        [TestMethod]
        public async Task LoadGLBFromStream()
        {
            IGLTFLoader gltfLoader = new GLTFLoader();
            Assert.IsTrue(File.Exists(GLB_PATH));
            FileStream gltfStream = File.OpenRead(GLB_PATH);
            GLTFRoot gltfRoot = await gltfLoader.Load(gltfStream);
            GLTFLoadTestHelper.TestGLB(gltfRoot);
        }
    }
}
