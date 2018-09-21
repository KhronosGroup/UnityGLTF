using System;
using GLTF;
using GLTF.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace GLTFSerializationTests
{
	[TestClass]
	public class MergeNodesTest
	{
		private readonly string GLTF_BOOMBOX_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
		private readonly string GLTF_LANTERN_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/Lantern.gltf";

		[TestMethod]
		public void MergeNodes()
		{
			Assert.IsTrue(File.Exists(GLTF_BOOMBOX_PATH));
			Assert.IsTrue(File.Exists(GLTF_LANTERN_PATH));
			
			FileStream gltfBoomBoxStream = File.OpenRead(GLTF_BOOMBOX_PATH);
			GLTFRoot boomBoxRoot;
			GLTFParser.ParseJson(gltfBoomBoxStream, out boomBoxRoot);

			FileStream gltfLanternStream = File.OpenRead(GLTF_LANTERN_PATH);
			GLTFRoot lanternRoot;
			GLTFParser.ParseJson(gltfLanternStream, out lanternRoot);

			GLTFRoot boomBoxCopy = new GLTFRoot(boomBoxRoot);

			GLTFHelpers.MergeGLTF(boomBoxRoot, lanternRoot);
			
			Assert.AreNotEqual(boomBoxRoot.Nodes, boomBoxCopy.Nodes);

			Assert.AreNotEqual(boomBoxCopy.Accessors.Count, boomBoxRoot.Accessors.Count);
			Assert.AreNotEqual(boomBoxCopy.Meshes.Count, boomBoxRoot.Meshes.Count);
			Assert.AreNotEqual(boomBoxCopy.Nodes.Count, boomBoxRoot.Nodes.Count);
			Assert.AreNotEqual(boomBoxCopy.BufferViews.Count, boomBoxRoot.BufferViews.Count);
			Assert.AreNotEqual(boomBoxCopy.Buffers.Count, boomBoxRoot.Buffers.Count);
			Assert.AreNotEqual(boomBoxCopy.Images.Count, boomBoxRoot.Images.Count);
			Assert.AreNotEqual(boomBoxCopy.Materials.Count, boomBoxRoot.Materials.Count);
			Assert.AreNotEqual(boomBoxCopy.Textures.Count, boomBoxRoot.Textures.Count);
			Assert.AreNotEqual(boomBoxCopy.Scenes.Count, boomBoxRoot.Scenes.Count);

			Assert.AreEqual(boomBoxCopy.Accessors.Count + lanternRoot.Accessors.Count, boomBoxRoot.Accessors.Count);
			Assert.AreEqual(boomBoxCopy.Meshes.Count + lanternRoot.Meshes.Count, boomBoxRoot.Meshes.Count);
			Assert.AreEqual(boomBoxCopy.Nodes.Count + lanternRoot.Nodes.Count, boomBoxRoot.Nodes.Count);
			Assert.AreEqual(boomBoxCopy.BufferViews.Count + lanternRoot.BufferViews.Count, boomBoxRoot.BufferViews.Count);
			Assert.AreEqual(boomBoxCopy.Buffers.Count + lanternRoot.Buffers.Count, boomBoxRoot.Buffers.Count);
			Assert.AreEqual(boomBoxCopy.Images.Count + lanternRoot.Images.Count, boomBoxRoot.Images.Count);
			Assert.AreEqual(boomBoxCopy.Materials.Count + lanternRoot.Materials.Count, boomBoxRoot.Materials.Count);
			Assert.AreEqual(boomBoxCopy.Textures.Count + lanternRoot.Textures.Count, boomBoxRoot.Textures.Count);
			Assert.AreEqual(boomBoxCopy.Scenes.Count + lanternRoot.Scenes.Count, boomBoxRoot.Scenes.Count);

			// test no throw
			StringWriter stringWriter = new StringWriter();
			boomBoxRoot.Serialize(stringWriter);
		}
	}
}
