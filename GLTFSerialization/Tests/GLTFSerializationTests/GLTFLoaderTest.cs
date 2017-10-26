using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.IO;
using GLTF;
using GLTF.Schema;
using GLTF.Math;
using System.Collections.Generic;

namespace GLTFSerializationTests
{
	[TestClass]
	public class GLTFJsonLoaderTest
    {
        readonly string GLTF_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
        readonly string GLTF_PBR_SPECGLOSS_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-pbrSpecularGlossiness/Lantern.gltf";
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
        public void LoadKHRSpecGlossGLTFFromStream()
        {
            Assert.IsTrue(File.Exists(GLTF_PBR_SPECGLOSS_PATH));
            FileStream gltfStream = File.OpenRead(GLTF_PBR_SPECGLOSS_PATH);
            int streamLength = (int)gltfStream.Length;
            byte[] gltfData = new byte[streamLength];
            gltfStream.Read(gltfData, 0, streamLength);
            
            GLTFRoot gltfRoot = GLTFParser.ParseJson(gltfData);

            Assert.IsNotNull(gltfRoot.ExtensionsUsed);
            Assert.IsTrue(gltfRoot.ExtensionsUsed.Contains(KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME));

            Assert.IsNotNull(gltfRoot.Materials);
            Assert.AreEqual(1, gltfRoot.Materials.Count);
            Material materialDef = gltfRoot.Materials[0];
            KHR_materials_pbrSpecularGlossinessExtension specGloss = materialDef.Extensions[KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME] as KHR_materials_pbrSpecularGlossinessExtension;
            Assert.IsTrue(specGloss != null);

            Assert.AreEqual(Color.White, specGloss.DiffuseFactor);
            Assert.AreEqual(4, specGloss.DiffuseTexture.Index.Id);
            Assert.AreEqual(KHR_materials_pbrSpecularGlossinessExtension.SPEC_FACTOR_DEFAULT, specGloss.SpecularFactor);
            Assert.AreEqual(KHR_materials_pbrSpecularGlossinessExtension.GLOSS_FACTOR_DEFAULT, specGloss.GlossinessFactor);
            Assert.AreEqual(5, specGloss.SpecularGlossinessTexture.Index.Id);
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
