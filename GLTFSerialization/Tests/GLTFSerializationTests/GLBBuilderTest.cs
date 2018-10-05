using GLTF;
using GLTF.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GLTFSerializationTests
{
	[TestClass]
	public class GLBBuilderTest
	{
		[TestMethod]
		public void CreateAndSaveFromEmptyStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 10, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream);
			Assert.IsNull(glbObject.Root);

			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(TestAssetPaths.MIN_GLTF_STR);
			writer.Flush();
			stream.Position = 0;

			GLTFRoot gltfRoot;
			GLTFParser.ParseJson(stream, out gltfRoot);
			GLBBuilder.SetRoot(glbObject, gltfRoot);

			GLBBuilder.UpdateStream(glbObject);

			glbStream.Close();
			glbStream = new FileStream(outPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

			glbObject = GLBBuilder.ConstructFromStream(glbStream);
			Assert.IsNotNull(glbObject.Root);
			Assert.AreEqual(glbObject.Root.Asset.Version, gltfRoot.Asset.Version);
			glbStream.Close();
		}

		[TestMethod]
		public void CreateEmptyStreamAndAppendGLB()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 11, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream);
			Assert.IsNull(glbObject.Root);

			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(TestAssetPaths.MIN_GLTF_STR);
			writer.Flush();
			stream.Position = 0;

			GLTFRoot gltfRoot;
			GLTFParser.ParseJson(stream, out gltfRoot);
			GLBBuilder.SetRoot(glbObject, gltfRoot);
			GLBBuilder.UpdateStream(glbObject);

			FileStream glbAppendStream = File.OpenRead(TestAssetPaths.GLB_BOOMBOX_PATH);
			GLBObject glbAppendObject = GLBBuilder.ConstructFromStream(glbAppendStream);
			GLBBuilder.AddBinaryData(glbObject, glbAppendStream, false, glbAppendObject.BinaryChunkInfo.StartPosition + GLTFParser.CHUNK_HEADER_SIZE);

			glbStream.Close();
			glbStream = new FileStream(outPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

			glbObject = GLBBuilder.ConstructFromStream(glbStream);
			Assert.IsNotNull(glbObject.Root);
			Assert.AreEqual(glbAppendObject.BinaryChunkInfo.Length, glbObject.BinaryChunkInfo.Length);
			glbStream.Close();
		}

		[TestMethod]
		public void CreateGLBFromStream()
		{
			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOOMBOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOOMBOX_PATH);
			FileStream glbOutStream = File.Create(TestAssetPaths.GLB_BOOMBOX_OUT_PATH);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			Assert.IsNotNull(glbObject.Root);
			Assert.IsNotNull(glbObject.Stream);
			Assert.AreEqual(0, glbObject.StreamStartPosition);
			Assert.AreEqual(GLTFParser.HEADER_SIZE, glbObject.JsonChunkInfo.StartPosition);
			Assert.AreEqual(glbStream.Length, glbObject.Header.FileLength);

			glbOutStream.Position = 0;
			GLTFRoot glbOutRoot;
			GLTFParser.ParseJson(glbOutStream, out glbOutRoot);
			GLTFJsonLoadTestHelper.TestGLB(glbOutRoot);
		}

		[TestMethod]
		public void UpdateStream()
		{
			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOOMBOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOOMBOX_PATH);
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 0, TestAssetPaths.GLB_EXTENSION);
			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

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

			GLBBuilder.UpdateStream(glbObject);
			glbOutStream.Position = 0;

			GLTFRoot glbOutRoot;
			GLTFParser.ParseJson(glbOutStream, out glbOutRoot);
			FileStream glbFileStream = glbObject.Stream as FileStream;
			Assert.AreEqual(glbFileStream, glbFileStream);
			glbOutStream.Position = 0;
			List<ChunkInfo> chunkInfo = GLTFParser.FindChunks(glbOutStream);
			Assert.AreEqual(2, chunkInfo.Count);
			CompareBinaryData(glbObject, glbStream);
		}

		[TestMethod]
		public void AddBinaryDataToStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 1, TestAssetPaths.GLB_EXTENSION);

			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			const int bufferSize = 101;
			byte[] buffer = new byte[bufferSize];
			AddBinaryDataToStreamHelper(glbObject, new MemoryStream(buffer));
		}

		[TestMethod]
		public void AddFirstBinaryDataToStream()
		{
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(TestAssetPaths.MIN_GLTF_STR);
			writer.Flush();
			stream.Position = 0;

			MemoryStream writeStream = new MemoryStream();
			GLBObject glbObject = GLBBuilder.ConstructFromStream(stream, writeStream);
			Assert.IsNull(glbObject.Root.Buffers);

			const uint bufferSize = 100;
			byte[] buffer = new byte[bufferSize];
			AddBinaryDataToStreamHelper(glbObject, new MemoryStream(buffer));
		}

		[TestMethod]
		public void RemoveBinaryDataFromStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 2, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			const uint bufferSize = 100;
			byte[] buffer = new byte[bufferSize];
			BufferViewId bufferViewId = AddBinaryDataToStreamHelper(glbObject, new MemoryStream(buffer));
			uint length = (uint)bufferViewId.Value.ByteLength;
			uint previousFileLength = glbObject.Header.FileLength;
			uint previousBufferLength = glbObject.BinaryChunkInfo.Length;
			int previousBufferViewCount = glbObject.Root.BufferViews.Count;
			GLBBuilder.RemoveBinaryData(glbObject, bufferViewId);
			Assert.AreEqual(previousFileLength - length, glbObject.Header.FileLength);
			Assert.AreEqual(previousBufferLength - length, glbObject.BinaryChunkInfo.Length);
			Assert.AreEqual(previousBufferLength - length, (uint)glbObject.Root.Buffers[0].ByteLength);
			Assert.AreEqual(previousBufferViewCount - 1, glbObject.Root.BufferViews.Count);
		}

		[TestMethod]
		public void RemoveMiddleDataFromStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 3, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);

			const uint numBuffersToAdd = 5;
			const uint bufferSize = 100;
			BufferViewId[] bufferViews = new BufferViewId[numBuffersToAdd];
			for (int i = 0; i < numBuffersToAdd; ++i)
			{
				byte[] buffer = new byte[bufferSize];
				bufferViews[i] = AddBinaryDataToStreamHelper(glbObject, new MemoryStream(buffer));
			}

			uint previousFileLength = glbObject.Header.FileLength;
			uint previousBufferLength = glbObject.BinaryChunkInfo.Length;
			int previousBufferViewCount = glbObject.Root.BufferViews.Count;
			GLBBuilder.RemoveBinaryData(glbObject, bufferViews[2]);	// remove from the middle
			Assert.AreEqual(previousFileLength, glbObject.Header.FileLength);
			Assert.AreEqual(previousBufferLength, glbObject.BinaryChunkInfo.Length);
			Assert.AreEqual(previousBufferViewCount - 1, glbObject.Root.BufferViews.Count);
		}

		[TestMethod]
		public void RemoveAllDataFromStream()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 3, TestAssetPaths.GLB_EXTENSION);

			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);

			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);
			BufferViewId id0 = new BufferViewId
			{
				Id = 0,
				Root = glbObject.Root
			};

			int numBufferViews = glbObject.Root.BufferViews.Count;
			for (int i = 0; i < numBufferViews; ++i)
			{
				GLBBuilder.RemoveBinaryData(glbObject, id0);
			}

			Assert.AreEqual(0, glbObject.Root.Buffers.Count);
			Assert.AreEqual(0, glbObject.Root.BufferViews.Count);
		}

		[TestMethod]
		public void MergeGLBs()
		{
			Assert.IsTrue(File.Exists(TestAssetPaths.GLB_BOX_PATH));
			FileStream glbStream = File.OpenRead(TestAssetPaths.GLB_BOX_PATH);
			FileStream glbStream1 = File.OpenRead(TestAssetPaths.GLB_BOOMBOX_PATH);
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 4, TestAssetPaths.GLB_EXTENSION);
			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbStream, glbOutStream);
			GLBObject glbObject1 = GLBBuilder.ConstructFromStream(glbStream1);
			uint initialGLBLength = glbObject.BinaryChunkInfo.Length;
			GLBBuilder.MergeGLBs(glbObject, glbObject1);

			Assert.AreEqual(initialGLBLength + glbObject1.BinaryChunkInfo.Length, glbObject.BinaryChunkInfo.Length);
		}

		[TestMethod]
		public void GLBSaveWithoutBinary()
		{
			string outPath =
				TestAssetPaths.GetOutPath(TestAssetPaths.GLB_BOX_OUT_PATH_TEMPLATE, 15, TestAssetPaths.GLB_EXTENSION);
			FileStream glbOutStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

			// first create from empty stream
			GLBObject glbObject = GLBBuilder.ConstructFromStream(glbOutStream);
			uint initialGLBLength = glbObject.BinaryChunkInfo.Length;

			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(TestAssetPaths.MIN_GLTF_STR);
			writer.Flush();
			stream.Position = 0;

			GLTFRoot newRoot;
			GLTFParser.ParseJson(stream, out newRoot);
			GLBBuilder.SetRoot(glbObject, newRoot);
			GLBBuilder.UpdateStream(glbObject);
			Assert.AreEqual(glbObject.Header.FileLength, glbObject.JsonChunkInfo.StartPosition + glbObject.JsonChunkInfo.Length + GLTFParser.CHUNK_HEADER_SIZE);
			glbOutStream.Close();
			glbOutStream = new FileStream(outPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			List<ChunkInfo> chunks = GLTFParser.FindChunks(glbOutStream);
			Assert.AreEqual(1, chunks.Count);

			glbOutStream.Position = 0;
			GLBObject testObject = GLBBuilder.ConstructFromStream(glbOutStream);

			Assert.AreEqual(glbObject.JsonChunkInfo.Length, testObject.JsonChunkInfo.Length);
			Assert.AreEqual(glbObject.BinaryChunkInfo.Length, testObject.BinaryChunkInfo.Length);
		}

		private BufferViewId AddBinaryDataToStreamHelper(GLBObject glbObject, Stream blobToAdd)
		{
			int previousCount = 0;
			if (glbObject.Root.BufferViews != null)
			{
				previousCount = glbObject.Root.BufferViews.Count;
			}

			uint previousGLBLength = glbObject.Header.FileLength;
			uint previousChunkLength = glbObject.BinaryChunkInfo.Length;

			uint bufferSize = GLBBuilder.CalculateAlignment((uint)blobToAdd.Length, 4);
			BufferViewId bufferViewId = GLBBuilder.AddBinaryData(glbObject, blobToAdd);

			uint headerModifier = previousChunkLength == 0 ? GLTFParser.CHUNK_HEADER_SIZE : 0;
			Assert.AreEqual(previousCount + 1, glbObject.Root.BufferViews.Count);
			Assert.AreEqual(previousCount, bufferViewId.Id);
			Assert.AreEqual(previousGLBLength + bufferSize + headerModifier, glbObject.Header.FileLength);
			Assert.AreEqual(previousChunkLength + bufferSize, glbObject.BinaryChunkInfo.Length);
			Assert.AreEqual(previousChunkLength + bufferSize, glbObject.Root.Buffers[0].ByteLength);
			Assert.AreEqual(glbObject.Header.FileLength, glbObject.Stream.Length);

			return bufferViewId;
		}

		private void CompareBinaryData(GLBObject resultObject, FileStream sourceStream)
		{
			MemoryStream outStream = new MemoryStream();
			GLBObject sourceGLB = GLBBuilder.ConstructFromStream(sourceStream, outStream);
			byte[] resultObjectBinary = new byte[resultObject.BinaryChunkInfo.Length];
			resultObject.Stream.Position = resultObject.BinaryChunkInfo.StartPosition;
			resultObject.Stream.Read(resultObjectBinary, 0, resultObjectBinary.Length);

			byte[] sourceObjectBinary = new byte[sourceGLB.BinaryChunkInfo.Length];
			sourceGLB.Stream.Position = sourceGLB.BinaryChunkInfo.StartPosition;
			sourceGLB.Stream.Read(sourceObjectBinary, 0, sourceObjectBinary.Length);

			Assert.IsTrue(resultObjectBinary.SequenceEqual(sourceObjectBinary));
		}
	}
}
