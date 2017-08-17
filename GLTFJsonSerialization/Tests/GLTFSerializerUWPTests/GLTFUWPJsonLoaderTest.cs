using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GLTFJsonSerialization;
using GLTFJsonSerializerTests;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;


namespace GLTFJsonSerializerUWPTests
{
    [TestClass]
    public class GLTFUWPJsonLoaderTest
    {
        //readonly string GLTF_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
        //readonly string GLB_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/BoomBox.glb";
        readonly string GLTF_PATH = @"ms-appx:///Assets/glTF/BoomBox.gltf";

        [TestMethod]
        public async Task LoadGLTFFromStreamUWP()
        {
            IGLTFJsonLoader gltfLoader = new GLTFJsonLoader();
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(GLTF_PATH));
            IRandomAccessStream gltfStream = await sampleFile.OpenAsync(FileAccessMode.Read);
            GLTFRoot gltfRoot = await gltfLoader.Load(gltfStream);
            GLTFJsonLoadTestHelper.TestGLTF(gltfRoot);
        }
    }
}
