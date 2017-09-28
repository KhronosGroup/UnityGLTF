using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GLTF;
using GLTFSerializationTests;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using GLTF.Schema;


namespace GLTFSerializerUWPTests
{
	[TestClass]
	public class GLTFUWPJsonLoaderTest
	{
		readonly string GLTF_PATH = @"ms-appx:///Assets/glTF/BoomBox.gltf";

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
	}
}
