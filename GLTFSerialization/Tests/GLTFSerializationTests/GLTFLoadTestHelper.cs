using System.Collections.Generic;
using GLTF.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace GLTFSerializationTests
{
	public class TestExtension : IExtension
	{
		public float Glossiness { get; set; }

		public IExtension Clone(GLTFRoot root)
		{
			return new TestExtension()
			{
				Glossiness = Glossiness
			};
		}

		public JProperty Serialize()
		{
			JProperty glossinessProperty = new JProperty("glossiness", Glossiness);
			JObject testExtensionObject = new JObject(glossinessProperty);
			JProperty testExtensionProperty = new JProperty("testExtension", testExtensionObject);
			return testExtensionProperty;
		}
	}

	public class TestExtensionFactory : ExtensionFactory
	{
		public TestExtensionFactory()
		{
			ExtensionName = "testExtension";
		}


		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			Assert.IsNotNull(extensionToken.Value["glossiness"]);
			float glossiness = (float)extensionToken.Value["glossiness"];
			Assert.AreEqual(.8, glossiness, .01f);

			return new TestExtension()
			{
				Glossiness = glossiness
			};
		}
	}

	class GLTFJsonLoadTestHelper
	{
		private static void TestAccessor(Accessor accessor, GLTFAccessorAttributeType type, uint count, GLTFComponentType componentType, int bufferViewId, List<float> max, List<float> min)
		{
			Assert.AreEqual(type, accessor.Type);
			Assert.AreEqual(count, accessor.Count);
			Assert.AreEqual(componentType, accessor.ComponentType);
			Assert.AreEqual(bufferViewId, accessor.BufferView.Id);
			Assert.AreEqual(min.Count, accessor.Min.Count);
			Assert.AreEqual(max.Count, accessor.Max.Count);

			for (int i = 0; i < max.Count; ++i)
			{
				Assert.AreEqual(max[i], accessor.Max[i], .000001f);
			}

			for (int i = 0; i < min.Count; ++i)
			{
				Assert.AreEqual(min[i], accessor.Min[i], .000001f);
			}
		}

		private static void TestAccessors(GLTFRoot gltfRoot)
		{
			List<Accessor> accessors = gltfRoot.Accessors;
			Assert.AreEqual(5, accessors.Count);
			TestAccessor(accessors[0], GLTFAccessorAttributeType.VEC2, 3575, GLTFComponentType.Float, 0, new List<float> { 0.9999003f, -0.0221377648f }, new List<float> { 0.0006585993f, -0.996773958f });
			TestAccessor(accessors[1], GLTFAccessorAttributeType.VEC3, 3575, GLTFComponentType.Float, 1, new List<float> { 1.0f, 1.0f, 0.9999782f }, new List<float> { -1.0f, -1.0f, -0.9980823f });
			TestAccessor(accessors[2], GLTFAccessorAttributeType.VEC4, 3575, GLTFComponentType.Float, 2, new List<float> { 1.0f, 0.9999976f, 1.0f, 1.0f }, new List<float> { -0.9991289f, -0.999907851f, -1.0f, 1.0f });
			TestAccessor(accessors[3], GLTFAccessorAttributeType.VEC3, 3575, GLTFComponentType.Float, 3, new List<float> { 0.009921154f, 0.00977163f, 0.0100762453f }, new List<float> { -0.009921154f, -0.00977163f, -0.0100762453f });
			TestAccessor(accessors[4], GLTFAccessorAttributeType.SCALAR, 18108, GLTFComponentType.UnsignedShort, 4, new List<float> { 3574f }, new List<float> { 0f });
		}

		private static void TestGLBAccessors(GLTFRoot gltfRoot)
		{
			List<Accessor> accessors = gltfRoot.Accessors;
			Assert.AreEqual(5, accessors.Count);
			TestAccessor(accessors[0], GLTFAccessorAttributeType.VEC2, 3575, GLTFComponentType.Float, 4, new List<float> { 0.9999003f, -0.0221377648f }, new List<float> { 0.0006585993f, -0.996773958f });
			TestAccessor(accessors[1], GLTFAccessorAttributeType.VEC3, 3575, GLTFComponentType.Float, 5, new List<float> { 1.0f, 1.0f, 0.9999782f }, new List<float> { -1.0f, -1.0f, -0.9980823f });
			TestAccessor(accessors[2], GLTFAccessorAttributeType.VEC4, 3575, GLTFComponentType.Float, 6, new List<float> { 1.0f, 0.9999976f, 1.0f, 1.0f }, new List<float> { -0.9991289f, -0.999907851f, -1.0f, 1.0f });
			TestAccessor(accessors[3], GLTFAccessorAttributeType.VEC3, 3575, GLTFComponentType.Float, 7, new List<float> { 0.009921154f, 0.00977163f, 0.0100762453f }, new List<float> { -0.009921154f, -0.00977163f, -0.0100762453f });
			TestAccessor(accessors[4], GLTFAccessorAttributeType.SCALAR, 18108, GLTFComponentType.UnsignedShort, 8, new List<float> { 3574f }, new List<float> { 0f });
		}

		private static void TestAssetData(GLTFRoot gltfRoot)
		{
			Assert.AreEqual("2.0", gltfRoot.Asset.Version);
			Assert.AreEqual("glTF Tools for Unity", gltfRoot.Asset.Generator);
		}

		private static void TestBufferView(BufferView bufferView, int buffer, uint byteOffset, uint byteLenth)
		{
			Assert.AreEqual(buffer, bufferView.Buffer.Id);
			Assert.AreEqual(byteOffset, bufferView.ByteOffset);
			Assert.AreEqual(byteLenth, bufferView.ByteLength);
		}

		private static void TestBufferViews(GLTFRoot gltfRoot)
		{
			List<BufferView> bufferViews = gltfRoot.BufferViews;
			Assert.AreEqual(5, gltfRoot.BufferViews.Count);
			TestBufferView(bufferViews[0], 0, 0, 28600);
			TestBufferView(bufferViews[1], 0, 28600, 42900);
			TestBufferView(bufferViews[2], 0, 71500, 57200);
			TestBufferView(bufferViews[3], 0, 128700, 42900);
			TestBufferView(bufferViews[4], 0, 171600, 36216);
		}

		private static void TestGLBBufferViews(GLTFRoot gltfRoot)
		{
			List<BufferView> bufferViews = gltfRoot.BufferViews;
			Assert.AreEqual(9, gltfRoot.BufferViews.Count);
			TestBufferView(bufferViews[0], 0, 0, 3285844);
			TestBufferView(bufferViews[1], 0, 3285844, 4775529);
			TestBufferView(bufferViews[2], 0, 8061373, 2845923);
			TestBufferView(bufferViews[3], 0, 10907296, 132833);
			TestBufferView(bufferViews[4], 0, 11040132, 28600);
			TestBufferView(bufferViews[5], 0, 11068732, 42900);
			TestBufferView(bufferViews[6], 0, 11111632, 57200);
			TestBufferView(bufferViews[7], 0, 11168832, 42900);
			TestBufferView(bufferViews[8], 0, 11211732, 36216);
		}

		private static void TestBuffers(GLTFRoot gltfRoot)
		{
			List<GLTFBuffer> buffers = gltfRoot.Buffers;
			Assert.AreEqual(1, buffers.Count);
			Assert.AreEqual("BoomBox.bin", buffers[0].Uri);
			Assert.AreEqual((uint)207816, buffers[0].ByteLength);
		}

		private static void TestGLBBuffers(GLTFRoot gltfRoot)
		{
			List<GLTFBuffer> buffers = gltfRoot.Buffers;
			Assert.AreEqual(1, buffers.Count);
			Assert.AreEqual((uint)11247948, buffers[0].ByteLength);
		}

		private static void TestImages(GLTFRoot gltfRoot)
		{
			List<GLTFImage> images = gltfRoot.Images;
			Assert.AreEqual(4, images.Count);
			Assert.AreEqual("BoomBox_baseColor.png", images[0].Uri);
			Assert.AreEqual("BoomBox_occlusionRoughnessMetallic.png", images[1].Uri);
			Assert.AreEqual("BoomBox_normal.png", images[2].Uri);
			Assert.AreEqual("BoomBox_emissive.png", images[3].Uri);
		}

		private static void TestGLBImages(GLTFRoot gltfRoot)
		{
			List<GLTFImage> images = gltfRoot.Images;
			Assert.AreEqual(4, images.Count);
			Assert.AreEqual(0, images[0].BufferView.Id);
			Assert.AreEqual(1, images[1].BufferView.Id);
			Assert.AreEqual(2, images[2].BufferView.Id);
			Assert.AreEqual(3, images[3].BufferView.Id);

			foreach (GLTFImage image in images)
			{
				Assert.AreEqual("image/png", image.MimeType);
			}
		}

		private static void TestMeshes(GLTFRoot gltfRoot)
		{
			List<GLTFMesh> meshes = gltfRoot.Meshes;
			Assert.AreEqual(1, meshes.Count);

			Assert.AreEqual("BoomBox", meshes[0].Name);
			List<MeshPrimitive> primitives = meshes[0].Primitives;
			Assert.AreEqual(1, primitives.Count);

			Assert.AreEqual(4, primitives[0].Indices.Id);
			Assert.AreEqual(0, primitives[0].Material.Id);

			var attributes = primitives[0].Attributes;
			Assert.IsTrue(attributes.ContainsKey("TEXCOORD_0"));
			Assert.AreEqual(0, attributes["TEXCOORD_0"].Id);

			Assert.IsTrue(attributes.ContainsKey("NORMAL"));
			Assert.AreEqual(1, attributes["NORMAL"].Id);

			Assert.IsTrue(attributes.ContainsKey("TANGENT"));
			Assert.AreEqual(2, attributes["TANGENT"].Id);

			Assert.IsTrue(attributes.ContainsKey("POSITION"));
			Assert.AreEqual(3, attributes["POSITION"].Id);
		}

		private static void TestMaterials(GLTFRoot gltfRoot)
		{
			List<GLTFMaterial> materials = gltfRoot.Materials;
			Assert.AreEqual(1, materials.Count);

			Assert.AreEqual(0, materials[0].PbrMetallicRoughness.BaseColorTexture.Index.Id);
			Assert.AreEqual(1, materials[0].PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id);

			Assert.AreEqual(2, materials[0].NormalTexture.Index.Id);
			Assert.AreEqual(1, materials[0].OcclusionTexture.Index.Id);

			Assert.AreEqual(1.0f, materials[0].EmissiveFactor.R);
			Assert.AreEqual(1.0f, materials[0].EmissiveFactor.G);
			Assert.AreEqual(1.0f, materials[0].EmissiveFactor.B);
			Assert.AreEqual(1.0f, materials[0].EmissiveFactor.A);

			Assert.AreEqual(3, materials[0].EmissiveTexture.Index.Id);
			Assert.AreEqual("BoomBox_Mat", materials[0].Name);

			var extensions = materials[0].Extensions;
			if (extensions != null)
			{
				Assert.AreEqual(1, extensions.Count);
			}
		}

		private static void TestNodes(GLTFRoot gltfRoot)
		{
			List<Node> nodes = gltfRoot.Nodes;
			Assert.AreEqual(1, nodes.Count);

			Node node = nodes[0];
			Assert.AreEqual(0, node.Mesh.Id);
			Assert.AreEqual("BoomBox", node.Name);

			JProperty extras = node.Extras as JProperty;
			if (extras != null)
			{
				Assert.AreEqual(JTokenType.Object, extras.Value.Type);

				JObject jObject = extras.Value as JObject;
				JToken testFloatProperty = jObject["nodeInfo"];
				Assert.AreEqual(JTokenType.Float, testFloatProperty.Type);
				Assert.AreEqual(1000.4f, testFloatProperty.Value<float>());

				JToken testHierarchyProperty = jObject["nodeHierarchy"];
				Assert.AreEqual(JTokenType.Object, testHierarchyProperty.Type);
				JToken testHierarchy1Property = (testHierarchyProperty as JObject)["nodeHierarchy1"];
				Assert.AreEqual(JTokenType.Object, testHierarchyProperty.Type);
			}
		}

		private static void TestScenes(GLTFRoot gltfRoot)
		{
			Assert.AreEqual(0, gltfRoot.Scene.Id);
			List<GLTFScene> scenes = gltfRoot.Scenes;
			Assert.AreEqual(1, scenes.Count);

			Assert.AreEqual(1, scenes[0].Nodes.Count);
			Assert.AreEqual(0, scenes[0].Nodes[0].Id);
		}

		private static void TestTextures(GLTFRoot gltfRoot)
		{
			List<GLTFTexture> textures = gltfRoot.Textures;
			Assert.AreEqual(4, textures.Count);

			Assert.AreEqual(0, textures[0].Source.Id);
			Assert.AreEqual(1, textures[1].Source.Id);
			Assert.AreEqual(2, textures[2].Source.Id);
			Assert.AreEqual(3, textures[3].Source.Id);
		}

		private static void TestExtras(GLTFRoot gltfRoot)
		{
			Assert.IsNotNull(gltfRoot.Extras);

			JObject jObject = gltfRoot.Extras as JObject;
			JToken testIntProperty = jObject["testint"];
			Assert.AreEqual(JTokenType.Integer, testIntProperty.Type);
			Assert.AreEqual(254, testIntProperty.Value<int>());

			JToken testStringProperty = jObject["teststring"];
			Assert.AreEqual(JTokenType.String, testStringProperty.Type);
			Assert.AreEqual("hello", testStringProperty.Value<string>());
		}

		public static void TestGLTF(GLTFRoot gltfRoot)
		{
			TestAccessors(gltfRoot);
			TestAssetData(gltfRoot);
			TestBufferViews(gltfRoot);
			TestBuffers(gltfRoot);
			TestImages(gltfRoot);
			TestMeshes(gltfRoot);
			TestMaterials(gltfRoot);
			TestNodes(gltfRoot);
			TestScenes(gltfRoot);
			TestTextures(gltfRoot);
			TestExtras(gltfRoot);
		}

		public static void TestGLB(GLTFRoot gltfRoot)
		{
			TestGLBAccessors(gltfRoot);
			TestAssetData(gltfRoot);
			TestGLBBufferViews(gltfRoot);
			TestGLBBuffers(gltfRoot);
			TestGLBImages(gltfRoot);
			TestMeshes(gltfRoot);
			TestMaterials(gltfRoot);
			TestNodes(gltfRoot);
			TestScenes(gltfRoot);
			TestTextures(gltfRoot);
		}
	}
}
