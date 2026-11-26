using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private List<Transform> _skinnedNodes;
		private Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh> _bakedMeshes;

		private void ExportSkinFromNode(Transform transform)
		{
			exportSkinFromNodeMarker.Begin();

			var go = transform.gameObject;
			var skin = transform.GetComponent<SkinnedMeshRenderer>();
			var mesh = GetMeshFromGameObject(go);
			UniquePrimitive key = new UniquePrimitive();
			key.Mesh = mesh;
			key.SkinnedMeshRenderer = skin;
			key.Materials = GetMaterialsFromGameObject(go);
			MeshId val;
			if (!_primOwner.TryGetValue(key, out val))
			{
				Debug.Log("No mesh found for skin on " + transform, transform);
				exportSkinFromNodeMarker.End();
				return;
			}
			GLTF.Schema.Skin gltfSkin = new Skin();

			// early out of this SkinnedMeshRenderer has no bones assigned (could be BlendShapes-only)
			if (skin.bones == null || skin.bones.Length == 0)
			{
				exportSkinFromNodeMarker.End();
				return;
			}

			var skinBones = skin.bones;
			var bindposes = mesh.bindposes;
			if (skinBones.Length != bindposes.Length)
			{
				Debug.LogWarning("SkinnedMeshRenderer " + transform + " has " + skinBones.Length + " bones but its mesh has " + bindposes.Length + " bindposes. Skin information will be skipped.", transform);
				exportSkinFromNodeMarker.End();
				return;
			}

			bool allBoneTransformNodesHaveBeenExported = true;
			for (int i = 0; i < skinBones.Length; ++i)
			{
				if (!skinBones[i])
				{
					continue;
				}
				var nodeId = GetObjectId(skinBones[i]);
				if (!_exportedTransforms.ContainsKey(nodeId))
				{
					allBoneTransformNodesHaveBeenExported = false;
					break;
				}
			}

			if (!allBoneTransformNodesHaveBeenExported)
			{
				Debug.LogWarning("Not all bones for SkinnedMeshRenderer " + transform + " were exported. Skin information will be skipped. Make sure the bones are active and enabled if you want to export them.", transform);
				exportSkinFromNodeMarker.End();
				return;
			}

			// Missing bones (e.g. deleted from an unpacked prefab) can't be left out, since JOINTS_0 and the bindposes
			// refer to bones by index. Each one is replaced with a new empty node (joints have to be unique) below a stand-in,
			// so all other joints keep their index.
			var standIn = skin.rootBone && _exportedTransforms.ContainsKey(GetObjectId(skin.rootBone)) ? skin.rootBone : transform;
			for (int i = 0; i < skinBones.Length; ++i)
			{
				int jointNodeId;
				if (skinBones[i])
				{
					jointNodeId = _exportedTransforms[GetObjectId(skinBones[i])];
				}
				else
				{
					Debug.LogWarning("Skin has null bone at index " + i + ": " + skin + ". Vertices weighted to it will follow " + standIn.name + " instead.", skin);
					jointNodeId = AddMissingBoneNode(standIn, i);
					// Keep the vertices where they are in the current pose, relative to the stand-in
					bindposes[i] = standIn.worldToLocalMatrix * transform.localToWorldMatrix;
				}

				gltfSkin.Joints.Add(
					new NodeId
					{
						Id = jointNodeId,
						Root = _root
					});
			}

			gltfSkin.InverseBindMatrices = ExportAccessor(bindposes);

			Vector4[] bones = boneWeightToBoneVec4(mesh.boneWeights);
			Vector4[] weights = boneWeightToWeightVec4(mesh.boneWeights);

			AccessorId sharedBones = null;
			AccessorId sharedWeights = null;
			
			if(val != null)
			{
				GLTF.Schema.GLTFMesh gltfMesh = _root.Meshes[val.Id];
				if(gltfMesh != null)
				{
					var accessors = _meshToPrims[mesh];
					if (accessors.aJoints0 != null)
						sharedBones = accessors.aJoints0;
					if (accessors.aWeights0 != null)
						sharedWeights = accessors.aWeights0;
					
					foreach (MeshPrimitive prim in gltfMesh.Primitives)
					{
						if (!prim.Attributes.ContainsKey("JOINTS_0"))
						{
							if (sharedBones != null)
								prim.Attributes.Add("JOINTS_0", sharedBones);
							else
							{
								var jointsAccessor = ExportAccessorUint(bones);
								jointsAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
								prim.Attributes.Add("JOINTS_0", jointsAccessor);
								sharedBones = jointsAccessor;
								accessors.aJoints0 = jointsAccessor;
								_meshToPrims[mesh] = accessors;
							}
						}

						if (!prim.Attributes.ContainsKey("WEIGHTS_0"))
						{
							if (sharedWeights != null)
								prim.Attributes.Add("WEIGHTS_0", sharedWeights);
							else
							{
								var weightsAccessor = ExportAccessor(weights);
								weightsAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
								prim.Attributes.Add("WEIGHTS_0", weightsAccessor);
								sharedWeights = weightsAccessor;
								accessors.aWeights0 = weightsAccessor;
								_meshToPrims[mesh] = accessors;
							}
						}
					}
				}
			}

			_root.Nodes[_exportedTransforms[GetObjectId(transform)]].Skin = new SkinId() { Id = _root.Skins.Count, Root = _root };
			_root.Skins.Add(gltfSkin);

			exportSkinFromNodeMarker.End();
		}

		/// <summary>
		/// Adds an empty node as child of an exported transform, at the same position, to be used as joint for a missing bone.
		/// </summary>
		private int AddMissingBoneNode(Transform parent, int boneIndex)
		{
			var node = new Node();
			if (ExportNames)
			{
				node.Name = "MissingBone_" + boneIndex;
			}

			var id = _root.Nodes.Count;
			_root.Nodes.Add(node);

			var parentNode = _root.Nodes[_exportedTransforms[GetObjectId(parent)]];
			if (parentNode.Children == null)
			{
				parentNode.Children = new List<NodeId>(1);
			}
			parentNode.Children.Add(new NodeId { Id = id, Root = _root });
			return id;
		}

		private UnityEngine.Mesh GetMeshFromGameObject(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshFilter>())
			{
				return gameObject.GetComponent<MeshFilter>().sharedMesh;
			}

			SkinnedMeshRenderer skinMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
			if (skinMesh)
			{
				if (!ExportAnimations && settings.BakeSkinnedMeshes)
				{
					if (!_bakedMeshes.ContainsKey(skinMesh))
					{
						UnityEngine.Mesh bakedMesh = new UnityEngine.Mesh();
						skinMesh.BakeMesh(bakedMesh);
						_bakedMeshes.Add(skinMesh, bakedMesh);
					}

					return _bakedMeshes[skinMesh];
				}

				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
			}

			return null;
		}

		private UnityEngine.Material[] GetMaterialsFromGameObject(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshRenderer>())
			{
				return gameObject.GetComponent<MeshRenderer>().sharedMaterials;
			}

			if (gameObject.GetComponent<SkinnedMeshRenderer>())
			{
				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			}

			return null;
		}

		private Vector4[] boneWeightToBoneVec4(BoneWeight[] bw)
		{
			Vector4[] bones = new Vector4[bw.Length];
			for (int i = 0; i < bw.Length; ++i)
			{
				bones[i] = new Vector4(bw[i].boneIndex0, bw[i].boneIndex1, bw[i].boneIndex2, bw[i].boneIndex3);
			}

			return bones;
		}

		private Vector4[] boneWeightToWeightVec4(BoneWeight[] bw)
		{
			Vector4[] weights = new Vector4[bw.Length];
			for (int i = 0; i < bw.Length; ++i)
			{
				weights[i] = new Vector4(bw[i].weight0, bw[i].weight1, bw[i].weight2, bw[i].weight3);
			}

			return weights;
		}

	}
}
