using System;
using GLTF;
using GLTF.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 0, TestAssetPaths.GLB_EXTENSION);
			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			FileStream glbOutStream2 = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
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

		[TestMethod]
		public async Task AddBlobToStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 1, TestAssetPaths.GLB_EXTENSION);

			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			const int bufferSize = 100;
			byte[] buffer = new byte[bufferSize];
			await AddBlobToStreamTestHelper(glbObject, new MemoryStream(buffer));
		}

		[TestMethod]
		public async Task AddFirstBlobToStream()
		{
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(TestAssetPaths.MIN_GLTF_STR);
			writer.Flush();
			stream.Position = 0;

			MemoryStream writeStream = new MemoryStream();
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(stream, writeStream);
			Assert.IsNull(glbObject.Root.Buffers);

			const uint bufferSize = 100;
			byte[] buffer = new byte[bufferSize];
			await AddBlobToStreamTestHelper(glbObject, new MemoryStream(buffer));
		}

		[TestMethod]
		public async Task RemoveBlobFromStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 2, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			const uint bufferSize = 100;
			byte[] buffer = new byte[bufferSize];
			BufferViewId bufferViewId = await AddBlobToStreamTestHelper(glbObject, new MemoryStream(buffer));
			uint length = (uint)bufferViewId.Value.ByteLength;
			uint previousFileLength = glbObject.Header.FileLength;
			uint previousBufferLength = glbObject.BinaryChunkInfo.Length;
			int previousBufferViewCount = glbObject.Root.BufferViews.Count;
			await GLBBuilder.RemoveBlob(glbObject, bufferViewId);
			Assert.AreEqual(previousFileLength - length, glbObject.Header.FileLength);
			Assert.AreEqual(previousBufferLength - length, glbObject.BinaryChunkInfo.Length);
			Assert.AreEqual(previousBufferLength - length, (uint)glbObject.Root.Buffers[0].ByteLength);
			Assert.AreEqual(previousBufferViewCount - 1, glbObject.Root.BufferViews.Count);
		}

		[TestMethod]
		public async Task RemoveMiddleBlobFromStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 3, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			const uint numBuffersToAdd = 5;
			const uint bufferSize = 100;
			BufferViewId[] bufferViews = new BufferViewId[numBuffersToAdd];
			for (int i = 0; i < numBuffersToAdd; ++i)
			{
				byte[] buffer = new byte[bufferSize];
				bufferViews[i] = await AddBlobToStreamTestHelper(glbObject, new MemoryStream(buffer));
			}

			uint previousFileLength = glbObject.Header.FileLength;
			uint previousBufferLength = glbObject.BinaryChunkInfo.Length;
			int previousBufferViewCount = glbObject.Root.BufferViews.Count;
			await GLBBuilder.RemoveBlob(glbObject, bufferViews[2]);	// remove from the middle
			Assert.AreEqual(previousFileLength, glbObject.Header.FileLength);
			Assert.AreEqual(previousBufferLength, glbObject.BinaryChunkInfo.Length);
			Assert.AreEqual(previousBufferViewCount - 1, glbObject.Root.BufferViews.Count);
		}

		[TestMethod]
		public async Task RemoveAllBlobsFromStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 3, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = await GLBBuilder.ConstructFromStream(glbStream, glbOutStream);
			BufferViewId id0 = new BufferViewId
			{
				Id = 0,
				Root = glbObject.Root
			};

			int numBufferViews = glbObject.Root.BufferViews.Count;
			for (int i = 0; i < numBufferViews; ++i)
			{
				await GLBBuilder.RemoveBlob(glbObject, id0);
			}

			Assert.AreEqual(0, glbObject.Root.Buffers.Count);
			Assert.AreEqual(0, glbObject.Root.BufferViews.Count);
		}
		
		private async Task<BufferViewId> AddBlobToStreamTestHelper(GLBObject glbObject, Stream blobToAdd)
		{
			
			int previousCount = 0;
			if (glbObject.Root.BufferViews != null)
			{
				previousCount = glbObject.Root.BufferViews.Count;
			}

			uint previousGLBLength = glbObject.Header.FileLength;
			uint previousChunkLength = glbObject.BinaryChunkInfo.Length;

			int bufferSize = (int)blobToAdd.Length;
			BufferViewId bufferViewId = await GLBBuilder.AddBinaryData(glbObject, blobToAdd);
			
			Assert.AreEqual(previousCount + 1, glbObject.Root.BufferViews.Count);
			Assert.AreEqual(previousCount, bufferViewId.Id);
			Assert.AreEqual(previousGLBLength + bufferSize, glbObject.Header.FileLength);
			Assert.AreEqual(previousChunkLength + bufferSize, glbObject.BinaryChunkInfo.Length);
			Assert.AreEqual(previousChunkLength + bufferSize, glbObject.Root.Buffers[0].ByteLength);
			Assert.AreEqual(glbObject.Header.FileLength, glbObject.Stream.Length);

			return bufferViewId;
		}
	}
}
