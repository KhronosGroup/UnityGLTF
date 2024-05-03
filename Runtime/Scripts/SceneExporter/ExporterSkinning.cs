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

			bool allBoneTransformNodesHaveBeenExported = true;
			for (int i = 0; i < skin.bones.Length; ++i)
			{
				if (!skin.bones[i])
				{
					Debug.LogWarning("Skin has null bone at index " + i + ": " + skin, skin);
					continue;
				}
				var nodeId = skin.bones[i].GetInstanceID();
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

			for (int i = 0; i < skin.bones.Length; ++i)
			{
				if (!skin.bones[i])
				{
					continue;
				}

				var nodeId = skin.bones[i].GetInstanceID();

				gltfSkin.Joints.Add(
					new NodeId
					{
						Id = _exportedTransforms[nodeId],
						Root = _root
					});
			}

			gltfSkin.InverseBindMatrices = ExportAccessor(mesh.bindposes);

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

			_root.Nodes[_exportedTransforms[transform.GetInstanceID()]].Skin = new SkinId() { Id = _root.Skins.Count, Root = _root };
			_root.Skins.Add(gltfSkin);

			exportSkinFromNodeMarker.End();
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
