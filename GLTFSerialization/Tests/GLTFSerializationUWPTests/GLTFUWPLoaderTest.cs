using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GLTF;
using GLTFSerializationTests;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using GLTF.Schema;
using GLTF.Math;

namespace GLTFSerializerUWPTests
{
	[TestClass]
	public class GLTFUWPJsonLoaderTest
	{
		readonly string GLTF_PATH = @"ms-appx:///Assets/glTF/BoomBox.gltf";
        readonly string GLTF_PBR_SPECGLOSS_PATH = @"ms-appx:///Assets/glTF/Lantern.gltf";

        [TestMethod]
		public async Task LoadGLTFFromStreamUWP()
		{
			StorageFolder localFolder = ApplicationData.Current.LocalFolder;
			StorageFile sampleFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(GLTF_PATH));


			IRandomAccessStream gltfStream = await sampleFile.OpenAsync(FileAccessMode.Read);
			var reader = new DataReader(gltfStream.GetInputStreamAt(0));
			var bytes = new byte[gltfStream.Size];
			await reader.LoadAsync((uint)gltfStream.Size);
			reader.ReadBytes(bytes);
			GLTFRoot gltfRoot = GLTFParser.ParseJson(bytes);
			GLTFJsonLoadTestHelper.TestGLTF(gltfRoot);
		}

        [TestMethod]
        public async Task LoadKHRSpecGlossGLTFFromStreamUWP()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(GLTF_PBR_SPECGLOSS_PATH));


            IRandomAccessStream gltfStream = await sampleFile.OpenAsync(FileAccessMode.Read);
            var reader = new DataReader(gltfStream.GetInputStreamAt(0));
            var bytes = new byte[gltfStream.Size];
            await reader.LoadAsync((uint)gltfStream.Size);
            reader.ReadBytes(bytes);
            GLTFRoot gltfRoot = GLTFParser.ParseJson(bytes);

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
    }
}
