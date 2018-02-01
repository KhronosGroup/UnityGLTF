using GLTF;
using GLTF.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using UnityGLTF;

namespace GLTFSerializationTests
{
	[TestClass]
	public class GLBBuilderTest
	{
		[TestMethod]
		public async Task CreateGLBFromStream()
		{
			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOOMBOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOOMBOX_PATH);
			FileStream glbOutStream = File.Create(TestAssetPaths.GLB_BOOMBOX_OUT_PATH);
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			Assert.IsNotNull(glbObject.Root);
			Assert.IsNotNull(glbObject.Stream);
			Assert.AreEqual(0, glbObject.StreamStartPosition);
			Assert.AreEqual(GLTFParser.HEADER_SIZE, glbObject.JsonChunkInfo.StartPosition);
			Assert.AreEqual(glbStream.Length, glbObject.Header.FileLength);

			glbOutStream.Position = 0;
			GLTFRoot glbOutRoot = GLTFParser.ParseJson(glbOutStream);
			GLTFJsonLoadTestHelper.TestGLB(glbOutRoot);
		}

		[TestMethod]
		public async Task UpdateStream()
		{
			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);
			FileStream glbOutStream = new FileStream(TestAssetPaths.GLB_BOX_OUT_PATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			FileStream glbOutStream2 = new FileStream(TestAssetPaths.GLB_BOX_OUT_PATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			for (int i = 0; i < 10; ++i)
			{
				glbObject.Root.Nodes.Add(new Node
				{
					Mesh = new MeshId
					{
						Id = 0,
						Root = glbObject.Root
					}
				});
			}

			await GLBBuilder.UpdateStream(glbObject, glbOutStream2);
			glbOutStream.Position = 0;
			GLTFParser.ParseJson(glbOutStream);
			FileStream glbFileStream = glbObject.Stream as FileStream;
			Assert.AreEqual(glbOutStream2, glbFileStream);
			glbOutStream.Position = 0;
			Assert.AreEqual(2, GLTFParser.FindChunks(glbOutStream).Count);
		}
	}
}
