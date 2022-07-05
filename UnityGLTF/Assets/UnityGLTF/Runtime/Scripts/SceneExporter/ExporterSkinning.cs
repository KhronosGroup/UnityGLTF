using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private void ExportSkinFromNode(Transform transform)
		{
			exportSkinFromNodeMarker.Begin();

			PrimKey key = new PrimKey();
			UnityEngine.Mesh mesh = GetMeshFromGameObject(transform.gameObject);
			key.Mesh = mesh;
			key.Materials = GetMaterialsFromGameObject(transform.gameObject);
			MeshId val;
			if (!_primOwner.TryGetValue(key, out val))
			{
				Debug.Log("No mesh found for skin on " + transform, transform);
				exportSkinFromNodeMarker.End();
				return;
			}
			SkinnedMeshRenderer skin = transform.GetComponent<SkinnedMeshRenderer>();
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

			if(val != null)
			{
				GLTF.Schema.GLTFMesh gltfMesh = _root.Meshes[val.Id];
				if(gltfMesh != null)
				{
					foreach (MeshPrimitive prim in gltfMesh.Primitives)
					{
						if (!prim.Attributes.ContainsKey("JOINTS_0"))
						{
							var jointsAccessor = ExportAccessorUint(bones);
							jointsAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
							prim.Attributes.Add("JOINTS_0", jointsAccessor);
						}

						if (!prim.Attributes.ContainsKey("WEIGHTS_0"))
						{
							var weightsAccessor = ExportAccessor(weights);
							weightsAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
							prim.Attributes.Add("WEIGHTS_0", weightsAccessor);
						}
					}
				}
			}

			_root.Nodes[_exportedTransforms[transform.GetInstanceID()]].Skin = new SkinId() { Id = _root.Skins.Count, Root = _root };
			_root.Skins.Add(gltfSkin);

			exportSkinFromNodeMarker.End();
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
