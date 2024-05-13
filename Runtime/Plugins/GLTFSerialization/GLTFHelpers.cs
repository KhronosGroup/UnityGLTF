using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTF.Schema;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace GLTF
{
	public static class GLTFHelpers
	{
		private struct PreviousGLTFSizes
		{
			public int PreviousBufferCount;
			public int PreviousBufferViewCount;
			public int PreviousAccessorCount;
			public int PreviousMeshCount;
			public int PreviousNodeCount;
			public int PreviousSceneCount;
			public int PreviousSkinCount;
			public int PreviousAnimationCount;
			public int PreviousCameraCount;
			public int PreviousMaterialCount;
			public int PreviousTextureCount;
			public int PreviousImageCount;
			public int PreviousSamplerCount;
		}

		/// <summary>
		/// Removes references to indexes that do not exist.
		/// </summary>
		/// <param name="root">The node to clean</param>
		public static void RemoveUndefinedReferences(GLTFRoot root)
		{
			int accessorCount = root.Accessors?.Count ?? 0;
			int bufferCount = root.Buffers?.Count ?? 0;
			int bufferViewCount = root.BufferViews?.Count ?? 0;
			int cameraCount = root.Cameras?.Count ?? 0;
			int meshCount = root.Meshes?.Count ?? 0;
			int nodeCount = root.Nodes?.Count ?? 0;
			int samplersCount = root.Samplers?.Count ?? 0;
			int skinCount = root.Skins?.Count ?? 0;
			int textureCount = root.Textures?.Count ?? 0;

			if (root.Accessors != null)
			{
				foreach (Accessor accessor in root.Accessors)
				{
					if (accessor.BufferView != null && accessor.BufferView.Id >= bufferViewCount)
					{
						accessor.BufferView = null;
					}
				}
			}
			if (root.Animations != null)
			{
				foreach (GLTFAnimation animation in root.Animations)
				{
					if (animation.Samplers != null)
					{
						foreach (AnimationSampler animationSampler in animation.Samplers)
						{
							if (animationSampler.Input != null && animationSampler.Input.Id >= accessorCount)
							{
								animationSampler.Input = null;
							}
							if (animationSampler.Output != null && animationSampler.Output.Id >= accessorCount)
							{
								animationSampler.Output = null;
							}
						}
					}
				}
			}
			if (root.BufferViews != null)
			{
				foreach (BufferView bufferView in root.BufferViews)
				{
					if (bufferView.Buffer != null && bufferView.Buffer.Id >= bufferCount)
					{
						bufferView.Buffer = null;
					}
				}
			}
			if (root.Images != null)
			{
				foreach (GLTFImage image in root.Images)
				{
					if (image.BufferView != null && image.BufferView.Id >= bufferViewCount)
					{
						image.BufferView = null;
					}
				}
			}
			if (root.Materials != null)
			{
				foreach (GLTFMaterial material in root.Materials)
				{
					if (material.EmissiveTexture?.Index != null && material.EmissiveTexture.Index.Id >= textureCount)
					{
						material.EmissiveTexture.Index = null;
					}
					if (material.NormalTexture?.Index != null && material.NormalTexture.Index.Id >= textureCount)
					{
						material.NormalTexture.Index = null;
					}
					if (material.OcclusionTexture?.Index != null && material.OcclusionTexture.Index.Id >= textureCount)
					{
						material.OcclusionTexture.Index = null;
					}
					if (material.OcclusionTexture?.Index != null && material.OcclusionTexture.Index.Id >= textureCount)
					{
						material.OcclusionTexture.Index = null;
					}
					if (material.PbrMetallicRoughness != null)
					{
						if (material.PbrMetallicRoughness.BaseColorTexture?.Index != null && material.PbrMetallicRoughness.BaseColorTexture.Index.Id >= textureCount)
						{
							material.PbrMetallicRoughness.BaseColorTexture.Index = null;
						}
						if (material.PbrMetallicRoughness.MetallicRoughnessTexture?.Index != null && material.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id >= textureCount)
						{
							material.PbrMetallicRoughness.MetallicRoughnessTexture.Index = null;
						}
					}
				}
			}
			if (root.Meshes != null)
			{
				foreach (GLTFMesh mesh in root.Meshes)
				{
					if (mesh.Primitives != null)
					{
						foreach (MeshPrimitive primitive in mesh.Primitives)
						{
							if (primitive.Indices != null && primitive.Indices.Id >= accessorCount)
							{
								primitive.Indices = null;
							}
							if (primitive.Material != null && primitive.Material.Id >= accessorCount)
							{
								primitive.Material = null;
							}
						}
					}
				}
			}
			if (root.Nodes != null)
			{
				foreach (Node node in root.Nodes)
				{
					if (node.Camera != null && node.Camera.Id >= cameraCount)
					{
						node.Camera = null;
					}
					if (node.Children != null)
					{
						for (int i = node.Children.Count - 1; i > 0; i--)
						{
							if (node.Children[i].Id >= nodeCount)
							{
								node.Children.RemoveAt(i);
							}
						}
					}
					if (node.Mesh != null && node.Mesh.Id >= meshCount)
					{
						node.Mesh = null;
					}
					if (node.Skin != null && node.Skin.Id >= skinCount)
					{
						node.Skin = null;
					}
				}
			}
			if (root.Scenes != null)
			{
				foreach (GLTFScene scene in root.Scenes)
				{
					if (scene.Nodes != null)
					{
						for (int i = scene.Nodes.Count - 1; i > 0; i--)
						{
							if (scene.Nodes[i].Id >= nodeCount)
							{
								scene.Nodes.RemoveAt(i);
							}
						}
					}
				}
			}
			if (root.Skins != null)
			{
				foreach (Skin skin in root.Skins)
				{
					if (skin.Joints != null)
					{
						for (int i = skin.Joints.Count - 1; i > 0; i--)
						{
							if (skin.Joints[i].Id >= nodeCount)
							{
								skin.Joints.RemoveAt(i);
							}
						}
					}
					if (skin.Skeleton != null && skin.Skeleton.Id >= nodeCount)
					{
						skin.Skeleton = null;
					}
				}
			}
			if (root.Textures != null)
			{
				foreach (GLTFTexture texture in root.Textures)
				{
					if (texture.Sampler != null && texture.Sampler.Id >= samplersCount)
					{
						texture.Sampler = null;
					}
				}
			}
		}

		private static void ApplySparseAccessorsVec3(ref NumericArray resultArray, AttributeAccessor attributeAccessor, AttributeAccessor sparseValues, AttributeAccessor sparseIndices)
		{
			var sparseArrays = new NumericArray[2];

			// Values
			LoadBufferView(sparseValues.AccessorId.Value.Sparse.Values.BufferView.Value, sparseValues.Offset, sparseValues.bufferData, out NativeArray<byte> bufferViewCache1);

			// Indices
			LoadBufferView(sparseIndices.AccessorId.Value.Sparse.Indices.BufferView.Value, sparseIndices.Offset, sparseIndices.bufferData, out NativeArray<byte> bufferViewCache2);

			Accessor.AsSparseFloat3Array(attributeAccessor.AccessorId.Value, ref sparseArrays[0], bufferViewCache1, 0);
			Accessor.AsSparseUIntArray(attributeAccessor.AccessorId.Value, ref sparseArrays[1], bufferViewCache2, 0);

			var before = new NumericArray();
			before.AsFloat3s = new float3[resultArray.AsFloat3s.Length];
			System.Array.Copy(resultArray.AsFloat3s, before.AsFloat3s, before.AsFloat3s.Length);

			for (int j = 0; j < sparseArrays[1].AsUInts.Length; j++)
				before.AsFloat3s[sparseArrays[1].AsUInts[j]] = sparseArrays[0].AsFloat3s[j];

			resultArray = before;
		}
		
		/// <summary>
		/// Uses the accessor to parse the buffer into attributes needed to construct the mesh primitive
		/// </summary>
		/// <param name="attributes">A dictionary that contains a mapping of attribute name to data needed to parse</param>
		/// <summary>
		/// Uses the accessor to parse the buffer into attributes needed to construct the mesh primitive
		/// </summary>
		/// <param name="attributes">A dictionary that contains a mapping of attribute name to data needed to parse</param>
		public static void BuildMeshAttributes(ref Dictionary<string, AttributeAccessor> attributes,ref Dictionary<string, (AttributeAccessor sparseIndices, AttributeAccessor sparseValues)> sparseAccessors)
		{
			if (attributes.TryGetValue(SemanticProperties.POSITION, out var attributeAccessor))
			{
				NumericArray resultArray = attributeAccessor.AccessorContent;
				LoadBufferView(attributeAccessor, out NativeArray<byte> bufferViewCache);
				attributeAccessor.AccessorId.Value.AsVertexArray(ref resultArray, bufferViewCache, 0, attributeAccessor.AccessorId.Value.Normalized);

				if (sparseAccessors.TryGetValue(SemanticProperties.POSITION, out var sparseData))
					ApplySparseAccessorsVec3(ref resultArray, attributeAccessor, sparseData.sparseValues,
						sparseData.sparseIndices);

				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.TryGetValue(SemanticProperties.INDICES, out var attributeAccessorIndices))
			{
				NumericArray resultArray = attributeAccessorIndices.AccessorContent;
				LoadBufferView(attributeAccessorIndices, out NativeArray<byte> bufferViewCache);
				attributeAccessorIndices.AccessorId.Value.AsTriangles(ref resultArray, bufferViewCache);
				attributeAccessorIndices.AccessorContent = resultArray;
			}
			if (attributes.TryGetValue(SemanticProperties.NORMAL, out var attributeAccessorNormals))
			{
				NumericArray resultArray = attributeAccessorNormals.AccessorContent;
				LoadBufferView(attributeAccessorNormals, out NativeArray<byte> bufferViewCache);
				attributeAccessorNormals.AccessorId.Value.AsNormalArray(ref resultArray, bufferViewCache, 0, attributeAccessorNormals.AccessorId.Value.Normalized);

				if (sparseAccessors.TryGetValue(SemanticProperties.NORMAL, out var sparseData))
					ApplySparseAccessorsVec3(ref resultArray, attributeAccessorNormals, sparseData.sparseValues, sparseData.sparseIndices);

				attributeAccessorNormals.AccessorContent = resultArray;
			}

			for (int i = 0; i < SemanticProperties.TexCoord.Length; i++)
			{
				if (attributes.TryGetValue(SemanticProperties.TexCoord[i], out var attributeAccessorTexCoord))
				{
					NumericArray resultArray = attributeAccessorTexCoord.AccessorContent;
					LoadBufferView(attributeAccessorTexCoord, out NativeArray<byte> bufferViewCache);
					attributeAccessorTexCoord.AccessorId.Value.AsTexcoordArray(ref resultArray, bufferViewCache, 0, attributeAccessorTexCoord.AccessorId.Value.Normalized);

					// if (sparseAccessors.TryGetValue(SemanticProperties.TexCoord[0], out var sparseData))
					// 	ApplySparseAccessorsTexCoord(ref resultArray, attributeAccessor, sparseData.sparseValues, sparseData.sparseIndices);

					attributeAccessorTexCoord.AccessorContent = resultArray;
				}
			}

			for (int i = 0; i < SemanticProperties.Color.Length; i++)
			{
				if (attributes.TryGetValue(SemanticProperties.Color[i], out var attributeAccessorColor))
				{
					NumericArray resultArray = attributeAccessorColor.AccessorContent;
					LoadBufferView(attributeAccessorColor, out NativeArray<byte> bufferViewCache);
					attributeAccessorColor.AccessorId.Value.AsColorArray(ref resultArray, bufferViewCache, 0, attributeAccessorColor.AccessorId.Value.Normalized);

					// if (sparseAccessors.TryGetValue(SemanticProperties.Color[0], out var sparseData))
					// 	ApplySparseAccessorsColor(ref resultArray, attributeAccessor, sparseData.sparseValues, sparseData.sparseIndices);

					attributeAccessorColor.AccessorContent = resultArray;
				}
			}
			
			if (attributes.TryGetValue(SemanticProperties.TANGENT, out var attributeAccessorTangent))
			{
				NumericArray resultArray = attributeAccessorTangent.AccessorContent;
				LoadBufferView(attributeAccessorTangent, out NativeArray<byte> bufferViewCache);
				attributeAccessorTangent.AccessorId.Value.AsTangentArray(ref resultArray, bufferViewCache, 0, attributeAccessorTangent.AccessorId.Value.Normalized);

				// if (sparseAccessors.TryGetValue(SemanticProperties.TANGENT, out var sparseData))
				// 	ApplySparseAccessorsTangent(ref resultArray, attributeAccessor, sparseData.sparseValues, sparseData.sparseIndices);

				attributeAccessorTangent.AccessorContent = resultArray;
			}

			for (int i = 0; i < SemanticProperties.Weight.Length; i++)
			{
				if (attributes.TryGetValue(SemanticProperties.Weight[i], out var attributeAccessorWeight))
				{
					NumericArray resultArray = attributeAccessorWeight.AccessorContent;
					LoadBufferView(attributeAccessorWeight, out NativeArray<byte> bufferViewCache);
					attributeAccessorWeight.AccessorId.Value.AsFloat4Array(ref resultArray, bufferViewCache, 0, attributeAccessorWeight.AccessorId.Value.Normalized);
					attributeAccessorWeight.AccessorContent = resultArray;
				}
			}

			for (int i = 0; i < SemanticProperties.Joint.Length; i++)
			{
				if (attributes.TryGetValue(SemanticProperties.Joint[0], out var attributeAccessorJoint))
				{
					NumericArray resultArray = attributeAccessorJoint.AccessorContent;
					LoadBufferView(attributeAccessorJoint, out NativeArray<byte> bufferViewCache);
					attributeAccessorJoint.AccessorId.Value.AsFloat4Array(ref resultArray, bufferViewCache, 0, attributeAccessorJoint.AccessorId.Value.Normalized);
					attributeAccessorJoint.AccessorContent = resultArray;
				}
			}
		}

		public static void BuildTargetAttributes(ref Dictionary<string, AttributeAccessor> attributes, float importScale = 1f)
		{
			var hasScale = !Mathf.Approximately(importScale, 1f);
			
			foreach (var kvp in attributes)
			{
				var attributeAccessor = kvp.Value;
				NumericArray resultArray = attributeAccessor.AccessorContent;
				LoadBufferView(attributeAccessor, out NativeArray<byte> bufferViewCache);

				bool normalize = attributeAccessor.AccessorId.Value.Normalized;
				switch (kvp.Key)
				{
					case SemanticProperties.POSITION:
						if (hasScale)
						{
							float3 conversionScale = new float3(importScale, importScale, importScale);
							attributeAccessor.AccessorId.Value.AsFloat3ArrayConversion(ref resultArray, bufferViewCache, conversionScale, 0, normalize);
						}
						else
							attributeAccessor.AccessorId.Value.AsFloat3Array(ref resultArray, bufferViewCache, 0, normalize);
						break;
					case SemanticProperties.NORMAL:
					case SemanticProperties.TANGENT:
						attributeAccessor.AccessorId.Value.AsFloat3Array(ref resultArray, bufferViewCache, 0, normalize);
						break;
					default:
						throw new System.Exception($"Unrecognized morph target attribute {kvp.Key}");
				}

				attributeAccessor.AccessorContent = resultArray;
			}
		}

		public static void BuildBindPoseSamplers(ref AttributeAccessor attributeAccessor)
		{
			NumericArray resultArray = attributeAccessor.AccessorContent;
			LoadBufferView(attributeAccessor, out NativeArray<byte> bufferViewCache);
			attributeAccessor.AccessorId.Value.AsMatrix4x4Array(ref resultArray, bufferViewCache, 0, attributeAccessor.AccessorId.Value.Normalized);
			attributeAccessor.AccessorContent = resultArray;
		}

		/// <summary>
		/// Uses the accessor to parse the buffer into arrays needed to construct the animation
		/// </summary>
		/// <param name="samplers">A dictionary mapping AttributeAccessor lists to their target types
		public static void BuildAnimationSamplers(ref Dictionary<string, List<AttributeAccessor>> samplers)
		{
			foreach (var samplerSet in samplers)
			{
				foreach (var attributeAccessor in samplerSet.Value)
				{
					NumericArray resultArray = attributeAccessor.AccessorContent;
					LoadBufferView(attributeAccessor, out NativeArray<byte> bufferViewCache);

					switch (attributeAccessor.AccessorId.Value.Type)
					{
						case GLTFAccessorAttributeType.SCALAR:
							attributeAccessor.AccessorId.Value.AsFloatArray(ref resultArray, bufferViewCache);
							break;
						case GLTFAccessorAttributeType.VEC2:
							attributeAccessor.AccessorId.Value.AsFloat2Array(ref resultArray, bufferViewCache);
							break;
						case GLTFAccessorAttributeType.VEC3:
							attributeAccessor.AccessorId.Value.AsFloat3Array(ref resultArray, bufferViewCache);
							break;
						case GLTFAccessorAttributeType.VEC4:
							attributeAccessor.AccessorId.Value.AsFloat4Array(ref resultArray, bufferViewCache);
							break;
						case GLTFAccessorAttributeType.MAT2:
							Debug.LogWarning("Unsupported MAT2 animation sampler type");
							break;
						case GLTFAccessorAttributeType.MAT3:
							Debug.LogWarning("Unsupported MAT3 animation sampler type");
							break;
						case GLTFAccessorAttributeType.MAT4:
							attributeAccessor.AccessorId.Value.AsMatrix4x4Array(ref resultArray, bufferViewCache);
							break;
					}

					attributeAccessor.AccessorContent = resultArray;
				}
			}
		}

		/// <summary>
		/// Merges the right root into the left root
		/// This function combines all of the lists of objects on each glTF root together and updates the relative indicies
		/// All properties all merged except Asset and Default, which will stay "mergeToRoot"'s value
		/// </summary>
		/// <param name="mergeToRoot">The node to merge into</param>
		/// <param name="mergeFromRoot">The node to merge from</param>
		public static void MergeGLTF(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot)
		{
			PreviousGLTFSizes previousGLTFSize = new PreviousGLTFSizes()
			{
				PreviousAccessorCount = mergeToRoot.Accessors?.Count ?? 0,
				PreviousBufferCount = mergeToRoot.Buffers?.Count ?? 0,
				PreviousAnimationCount = mergeToRoot.Animations?.Count ?? 0,
				PreviousBufferViewCount = mergeToRoot.BufferViews?.Count ?? 0,
				PreviousCameraCount = mergeToRoot.Cameras?.Count ?? 0,
				PreviousImageCount = mergeToRoot.Images?.Count ?? 0,
				PreviousMaterialCount = mergeToRoot.Materials?.Count ?? 0,
				PreviousMeshCount = mergeToRoot.Meshes?.Count ?? 0,
				PreviousNodeCount = mergeToRoot.Nodes?.Count ?? 0,
				PreviousSamplerCount = mergeToRoot.Samplers?.Count ?? 0,
				PreviousSceneCount = mergeToRoot.Scenes?.Count ?? 0,
				PreviousSkinCount = mergeToRoot.Skins?.Count ?? 0,
				PreviousTextureCount = mergeToRoot.Textures?.Count ?? 0
			};

			GLTFRoot mergeFromRootCopy = new GLTFRoot(mergeFromRoot);

			// for each type:
			// 1) add the right hand range to the left hand object
			// 2) update all ids to be based off of the appended size

			// merge extensions
			MergeExtensions(mergeToRoot, mergeFromRootCopy);

			// merge accessors, buffers, and bufferviews
			MergeAccessorsBufferViewsAndBuffers(mergeToRoot, mergeFromRootCopy, previousGLTFSize);

			// merge materials, samplers, images, and textures
			MergeMaterialsImagesTexturesAndSamplers(mergeToRoot, mergeFromRootCopy, previousGLTFSize);

			// merge meshes
			MergeMeshes(mergeToRoot, mergeFromRootCopy, previousGLTFSize);

			// merge cameras
			MergeCameras(mergeToRoot, mergeFromRootCopy);

			// merge nodes
			MergeNodes(mergeToRoot, mergeFromRootCopy, previousGLTFSize);

			// merge animation, and skin
			MergeAnimationsAndSkins(mergeToRoot, mergeFromRootCopy, previousGLTFSize);

			// merge scenes
			MergeScenes(mergeToRoot, mergeFromRootCopy, previousGLTFSize);
		}

		/// <summary>
		/// Returns whether the input string is a Base64 uri. Images and buffers can both be encoded this way.
		/// </summary>
		/// <param name="uri">The uri to check</param>
		/// <returns>Whether the input string is base64 encoded</returns>
		public static bool IsBase64Uri(string uri)
		{
			const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

			Regex regex = new Regex(Base64StringInitializer);
			return regex.Match(uri).Success;
		}

		private static void LoadBufferView(AttributeAccessor attributeAccessor, out byte[] bufferViewCache)
		{
			bufferViewCache = new byte[attributeAccessor.AccessorId.Value.BufferView.Value.ByteLength];
			LoadBufferView(attributeAccessor.AccessorId.Value.BufferView.Value, attributeAccessor.Offset, attributeAccessor.bufferData, out NativeArray<byte> bufferViewCacheNative);
			bufferViewCacheNative.CopyTo(bufferViewCache);
		}
		
		private static void LoadBufferView(AttributeAccessor attributeAccessor, out NativeArray<byte> bufferViewCache)
		{
			LoadBufferView(attributeAccessor.AccessorId.Value.BufferView.Value, attributeAccessor.Offset, attributeAccessor.bufferData, out bufferViewCache);
		}
		
		internal static void LoadBufferView(BufferView bufferView, uint Offset, NativeArray<byte> nativeBuffer, out NativeArray<byte> bufferViewCache)
		{
			uint totalOffset = bufferView.ByteOffset + Offset;
			bufferViewCache = nativeBuffer.GetSubArray((int)totalOffset, (int)bufferView.ByteLength);
		}
		
		private static void MergeExtensions(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot)
		{
			// avoid duplicates for extension merging
			if (mergeFromRoot.ExtensionsUsed != null)
			{
				if (mergeToRoot.ExtensionsUsed == null)
				{
					mergeToRoot.ExtensionsUsed = new List<string>(mergeFromRoot.ExtensionsUsed.Count);
				}

				foreach (string extensionUsedToAdd in mergeFromRoot.ExtensionsUsed)
				{
					if (!mergeToRoot.ExtensionsUsed.Contains(extensionUsedToAdd))
					{
						mergeToRoot.ExtensionsUsed.Add(extensionUsedToAdd);
					}
				}
			}

			if (mergeFromRoot.ExtensionsRequired != null)
			{
				if (mergeToRoot.ExtensionsRequired == null)
				{
					mergeToRoot.ExtensionsRequired = new List<string>(mergeFromRoot.ExtensionsRequired.Count);
				}

				foreach (string extensionRequiredToAdd in mergeFromRoot.ExtensionsRequired)
				{
					if (!mergeToRoot.ExtensionsRequired.Contains(extensionRequiredToAdd))
					{
						mergeToRoot.ExtensionsRequired.Add(extensionRequiredToAdd);
					}
				}
			}
		}

		private static void MergeAccessorsBufferViewsAndBuffers(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot, PreviousGLTFSizes previousGLTFSizes)
		{
			bool isGLB = false;

			if (mergeFromRoot.Buffers != null)
			{
				if (mergeToRoot.Buffers == null)
				{
					mergeToRoot.Buffers = new List<GLTFBuffer>(mergeFromRoot.Buffers.Count);
				}

				foreach (GLTFBuffer buffer in mergeFromRoot.Buffers)
				{
					if (buffer.Uri != null)
					{
						mergeToRoot.Buffers.Add(buffer);
					}
					else
					{
						isGLB = true;	// assume glb is a uri is null
					}
				}
			}

			if (mergeFromRoot.BufferViews != null)
			{
				if (mergeToRoot.BufferViews == null)
				{
					mergeToRoot.BufferViews = new List<BufferView>(mergeFromRoot.BufferViews.Count);
				}

				mergeToRoot.BufferViews.AddRange(mergeFromRoot.BufferViews);
				for (int i = previousGLTFSizes.PreviousBufferViewCount; i < mergeToRoot.BufferViews.Count; ++i)
				{
					GLTFId<GLTFBuffer> bufferId = mergeToRoot.BufferViews[i].Buffer;
					if (!(isGLB && bufferId.Id == 0))   // if it is pointing a the special glb buffer (index 0 of a glb) then we dont want to adjust the buffer view, otherwise we do
					{
						// adjusting bufferview id based on merge amount
						bufferId.Id += previousGLTFSizes.PreviousBufferCount;
						bufferId.Root = mergeToRoot;
					}
				}
			}

			if (mergeFromRoot.Accessors != null)
			{
				if (mergeToRoot.Accessors == null)
				{
					mergeToRoot.Accessors = new List<Accessor>(mergeFromRoot.Accessors.Count);
				}

				mergeToRoot.Accessors.AddRange(mergeFromRoot.Accessors);
				for (int i = previousGLTFSizes.PreviousAccessorCount; i < mergeToRoot.Accessors.Count; ++i)
				{
					Accessor accessor = mergeToRoot.Accessors[i];

					if (accessor.BufferView != null)
					{
						BufferViewId bufferViewId = accessor.BufferView;
						bufferViewId.Id += previousGLTFSizes.PreviousBufferViewCount;
						bufferViewId.Root = mergeToRoot;
					}

					AccessorSparse accessorSparse = accessor.Sparse;
					if (accessorSparse != null)
					{
						BufferViewId indicesId = accessorSparse.Indices.BufferView;
						indicesId.Id += previousGLTFSizes.PreviousBufferViewCount;
						indicesId.Root = mergeToRoot;

						BufferViewId valuesId = accessorSparse.Values.BufferView;
						valuesId.Id += previousGLTFSizes.PreviousBufferViewCount;
						valuesId.Root = mergeToRoot;
					}
				}
			}
		}

		private static void MergeMaterialsImagesTexturesAndSamplers(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot, PreviousGLTFSizes previousGLTFSizes)
		{
			if (mergeFromRoot.Samplers != null)
			{
				if (mergeToRoot.Samplers == null)
				{
					mergeToRoot.Samplers = new List<Sampler>(mergeFromRoot.Samplers.Count);
				}

				mergeToRoot.Samplers.AddRange(mergeFromRoot.Samplers);
			}

			if (mergeFromRoot.Images != null)
			{
				if (mergeToRoot.Images == null)
				{
					mergeToRoot.Images = new List<GLTFImage>(mergeFromRoot.Images.Count);
				}

				mergeToRoot.Images.AddRange(mergeFromRoot.Images);
				for (int i = previousGLTFSizes.PreviousImageCount; i < mergeToRoot.Images.Count; ++i)
				{
					GLTFImage image = mergeToRoot.Images[i];
					if (image.BufferView != null)
					{
						BufferViewId bufferViewId = image.BufferView;
						bufferViewId.Id += previousGLTFSizes.PreviousBufferViewCount;
						bufferViewId.Root = mergeToRoot;
					}
				}
			}

			if (mergeFromRoot.Textures != null)
			{
				if (mergeToRoot.Textures == null)
				{
					mergeToRoot.Textures = new List<GLTFTexture>(mergeFromRoot.Textures.Count);
				}

				mergeToRoot.Textures.AddRange(mergeFromRoot.Textures);
				for (int i = previousGLTFSizes.PreviousTextureCount; i < mergeToRoot.Textures.Count; ++i)
				{
					GLTFTexture texture = mergeToRoot.Textures[i];

					if (texture.Sampler != null)
					{
						SamplerId samplerId = texture.Sampler;
						samplerId.Id += previousGLTFSizes.PreviousSamplerCount;
						samplerId.Root = mergeToRoot;
					}

					if (texture.Source != null)
					{
						ImageId samplerId = texture.Source;
						samplerId.Id += previousGLTFSizes.PreviousImageCount;
						samplerId.Root = mergeToRoot;
					}
				}
			}

			if (mergeFromRoot.Materials != null)
			{
				if (mergeToRoot.Materials == null)
				{
					mergeToRoot.Materials = new List<GLTFMaterial>(mergeFromRoot.Materials.Count);
				}

				mergeToRoot.Materials.AddRange(mergeFromRoot.Materials);
				for (int i = previousGLTFSizes.PreviousMaterialCount; i < mergeToRoot.Materials.Count; ++i)
				{
					GLTFMaterial material = mergeToRoot.Materials[i];

					PbrMetallicRoughness pbrMetallicRoughness = material.PbrMetallicRoughness;
					if (pbrMetallicRoughness != null)
					{
						if (pbrMetallicRoughness.BaseColorTexture != null)
						{
							TextureId textureId = pbrMetallicRoughness.BaseColorTexture.Index;
							textureId.Id += previousGLTFSizes.PreviousTextureCount;
							textureId.Root = mergeToRoot;
						}
						if (pbrMetallicRoughness.MetallicRoughnessTexture != null)
						{
							TextureId textureId = pbrMetallicRoughness.MetallicRoughnessTexture.Index;
							textureId.Id += previousGLTFSizes.PreviousTextureCount;
							textureId.Root = mergeToRoot;
						}
					}

					MaterialCommonConstant commonConstant = material.CommonConstant;
					if (commonConstant?.LightmapTexture != null)
					{
						TextureId textureId = material.CommonConstant.LightmapTexture.Index;
						textureId.Id += previousGLTFSizes.PreviousTextureCount;
						textureId.Root = mergeToRoot;
					}

					if (material.EmissiveTexture != null)
					{
						TextureId textureId = material.EmissiveTexture.Index;
						material.EmissiveTexture.Index.Id += previousGLTFSizes.PreviousTextureCount;
						textureId.Root = mergeToRoot;
					}

					if (material.NormalTexture != null)
					{
						TextureId textureId = material.NormalTexture.Index;
						textureId.Id += previousGLTFSizes.PreviousTextureCount;
						textureId.Root = mergeToRoot;
					}

					if (material.OcclusionTexture != null)
					{
						TextureId textureId = material.OcclusionTexture.Index;
						textureId.Id += previousGLTFSizes.PreviousTextureCount;
						textureId.Root = mergeToRoot;
					}
				}
			}
		}

		private static void MergeMeshes(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot, PreviousGLTFSizes previousGLTFSizes)
		{
			if (mergeFromRoot.Meshes == null) return;

			if (mergeToRoot.Meshes == null)
			{
				mergeToRoot.Meshes = new List<GLTFMesh>(mergeFromRoot.Meshes.Count);
			}

			mergeToRoot.Meshes.AddRange(mergeFromRoot.Meshes);
			for (int i = previousGLTFSizes.PreviousMeshCount; i < mergeToRoot.Meshes.Count; ++i)
			{
				GLTFMesh mesh = mergeToRoot.Meshes[i];
				if (mesh.Primitives != null)
				{
					foreach (MeshPrimitive primitive in mesh.Primitives)
					{
						foreach (var attributeAccessorPair in primitive.Attributes)
						{
							AccessorId accessorId = attributeAccessorPair.Value;
							accessorId.Id += previousGLTFSizes.PreviousAccessorCount;
							accessorId.Root = mergeToRoot;
						}

						if (primitive.Indices != null)
						{
							AccessorId accessorId = primitive.Indices;
							accessorId.Id += previousGLTFSizes.PreviousAccessorCount;
							accessorId.Root = mergeToRoot;
						}

						if (primitive.Material != null)
						{
							MaterialId materialId = primitive.Material;
							materialId.Id += previousGLTFSizes.PreviousMaterialCount;
							materialId.Root = mergeToRoot;
						}

						if (primitive.Targets != null)
						{
							foreach (Dictionary<string, AccessorId> targetsDictionary in primitive.Targets)
							{
								foreach (var targetsPair in targetsDictionary)
								{
									AccessorId accessorId = targetsPair.Value;
									accessorId.Id += previousGLTFSizes.PreviousAccessorCount;
									accessorId.Root = mergeToRoot;
								}
							}
						}
					}
				}
			}
		}

		private static void MergeCameras(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot)
		{
			if (mergeFromRoot.Cameras == null) return;
			if (mergeToRoot.Cameras == null)
			{
				mergeToRoot.Cameras = new List<GLTFCamera>(mergeFromRoot.Cameras.Count);
			}

			mergeToRoot.Cameras.AddRange(mergeFromRoot.Cameras);
		}

		private static void MergeNodes(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot, PreviousGLTFSizes previousGLTFSizes)
		{
			if (mergeFromRoot.Nodes == null) return;

			if (mergeToRoot.Nodes == null)
			{
				mergeToRoot.Nodes = new List<Node>(mergeFromRoot.Nodes.Count);
			}

			mergeToRoot.Nodes.AddRange(mergeFromRoot.Nodes);

			for (int i = previousGLTFSizes.PreviousNodeCount; i < mergeToRoot.Nodes.Count; ++i)
			{
				Node node = mergeToRoot.Nodes[i];
				if (node.Mesh != null)
				{
					MeshId meshId = node.Mesh;
					meshId.Id += previousGLTFSizes.PreviousMeshCount;
					node.Mesh.Root = mergeToRoot;
				}

				if (node.Camera != null)
				{
					CameraId cameraId = node.Camera;
					cameraId.Id += previousGLTFSizes.PreviousCameraCount;
					cameraId.Root = mergeToRoot;
				}

				if (node.Children != null)
				{
					foreach (NodeId child in node.Children)
					{
						child.Id += previousGLTFSizes.PreviousNodeCount;
						child.Root = mergeToRoot;
					}
				}

				if (node.Skin != null)
				{
					SkinId skinId = node.Skin;
					skinId.Id += previousGLTFSizes.PreviousSkinCount;
					skinId.Root = mergeToRoot;
				}
			}
		}

		private static void MergeAnimationsAndSkins(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot, PreviousGLTFSizes previousGLTFSizes)
		{
			if (mergeFromRoot.Skins != null)
			{
				if (mergeToRoot.Skins == null)
				{
					mergeToRoot.Skins = new List<Skin>(mergeFromRoot.Skins.Count);
				}

				mergeToRoot.Skins.AddRange(mergeFromRoot.Skins);
				for (int i = previousGLTFSizes.PreviousSkinCount; i < mergeToRoot.Skins.Count; ++i)
				{
					Skin skin = mergeToRoot.Skins[i];
					if (skin.InverseBindMatrices != null)
					{
						skin.InverseBindMatrices.Id += previousGLTFSizes.PreviousAccessorCount;
					}

					if (skin.Skeleton != null)
					{
						skin.Skeleton.Id += previousGLTFSizes.PreviousNodeCount;
					}

					if (skin.Joints != null)
					{
						foreach (NodeId joint in skin.Joints)
						{
							joint.Id += previousGLTFSizes.PreviousNodeCount;
						}
					}
				}
			}

			if (mergeFromRoot.Animations != null)
			{
				if (mergeToRoot.Animations == null)
				{
					mergeToRoot.Animations = new List<GLTFAnimation>(mergeFromRoot.Animations.Count);
				}

				mergeToRoot.Animations.AddRange(mergeFromRoot.Animations);

				for (int i = previousGLTFSizes.PreviousAnimationCount; i < mergeToRoot.Animations.Count; ++i)
				{
					GLTFAnimation animation = mergeToRoot.Animations[i];
					foreach (AnimationSampler sampler in animation.Samplers)
					{
						AccessorId inputId = sampler.Input;
						inputId.Id += previousGLTFSizes.PreviousAccessorCount;
						inputId.Root = mergeToRoot;

						AccessorId outputId = sampler.Output;
						outputId.Id += previousGLTFSizes.PreviousAccessorCount;
						outputId.Root = mergeToRoot;
					}

					foreach (AnimationChannel channel in animation.Channels)
					{
						AnimationSamplerId samplerId = channel.Sampler;
						samplerId.Id += previousGLTFSizes.PreviousSamplerCount;
						samplerId.Root = mergeToRoot;

						NodeId nodeId = channel.Target.Node;
						nodeId.Id += previousGLTFSizes.PreviousNodeCount;
						nodeId.Root = mergeToRoot;
					}
				}
			}
		}

		private static void MergeScenes(GLTFRoot mergeToRoot, GLTFRoot mergeFromRoot, PreviousGLTFSizes previousGLTFSizes)
		{
			if (mergeFromRoot.Scenes == null) return;

			if (mergeToRoot.Scenes == null)
			{
				mergeToRoot.Scenes = new List<GLTFScene>(mergeFromRoot.Scenes.Count);
			}

			mergeToRoot.Scenes.AddRange(mergeFromRoot.Scenes);
			for (int i = previousGLTFSizes.PreviousSceneCount; i < mergeToRoot.Scenes.Count; ++i)
			{
				GLTFScene scene = mergeToRoot.Scenes[i];
				foreach (NodeId nodeId in scene.Nodes)
				{
					nodeId.Id += previousGLTFSizes.PreviousNodeCount;
					nodeId.Root = mergeToRoot;
				}
			}
		}

		private static string UpdateCanonicalPath(string oldPath, string newCanonicalPath)
		{
			string fileName = Path.GetFileName(oldPath);
			return newCanonicalPath + Path.DirectorySeparatorChar + fileName;
		}

		public static NodeId FindCommonAncestor(IEnumerable<NodeId> nodes)
		{
			// build parentage
			GLTFRoot root = nodes.First().Root;
			Dictionary<int, int> childToParent = new Dictionary<int, int>(root.Nodes.Count);
			for (int i = 0; i < root.Nodes.Count; i++)
			{
				if (root.Nodes[i].Children == null)
				{
					continue;
				}

				foreach (NodeId child in root.Nodes[i].Children)
				{
					childToParent[child.Id] = i;
				}
			}

			// scan for common ancestor
			int? commonAncestorIndex = nodes
				.Select(n => n.Id)
				.Aggregate((int?)null, (elder, node) => FindCommonAncestor(elder, node));

			return commonAncestorIndex != null ? new NodeId() { Id = commonAncestorIndex.Value, Root = root } : null;

			int? FindCommonAncestor(int? a, int? b)
			{
				// trivial cases
				if (a == null && b == null)
				{
					return null;
				}
				else if (a != null)
				{
					return a;
				}
				else if (b != null)
				{
					return b;
				}
				else if (AncestorOf(a.Value, b.Value))
				{
					return a;
				}
				else
				{
					return FindCommonAncestor(childToParent[a.Value], b.Value);
				}
			}

			bool AncestorOf(int ancestor, int descendant)
			{
				while (childToParent.ContainsKey(descendant))
				{
					if (childToParent[descendant] == ancestor)
					{
						return true;
					}
					descendant = childToParent[descendant];
				}

				return false;
			}
		}
	}
}
