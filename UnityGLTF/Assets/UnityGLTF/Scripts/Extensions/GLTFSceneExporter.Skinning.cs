using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using System.Linq;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private static readonly Matrix4x4 InvertZMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
		private static readonly Matrix4x4 InvertZMatrixInverse = InvertZMatrix.inverse;

		//exported skins --> skinIds
		private readonly Dictionary<Skin, SkinId> _skinCache = new Dictionary<Skin, SkinId>();

		private void ExportSkins(IEnumerable<Transform> nodeTransforms)
		{
			foreach (var transform in nodeTransforms)
			{
				GameObject gameObject = transform.gameObject;
				foreach (var skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
				{
					//grab the Unity node this skinned mesh renderer belongs to
					var nodeId = this._nodeCache[skinnedMeshRenderer.transform];
					//if: exist bind poses + a bone, OR exist bones
					if (skinnedMeshRenderer.sharedMesh.bindposes != null && skinnedMeshRenderer.sharedMesh.bindposes.Any() &&
						skinnedMeshRenderer.rootBone != null ||
						skinnedMeshRenderer.bones != null && skinnedMeshRenderer.bones.Any())
					{
						//grab exorted node, and add to it its skin
						_root.Nodes[nodeId.Id].Skin = ExportSkin(skinnedMeshRenderer);
					}
				}
			}
		}

		private SkinId ExportSkin(SkinnedMeshRenderer skinnedMeshRenderer)
		{
			var skin = new Skin
			{
				BindPoses = skinnedMeshRenderer.sharedMesh.bindposes,
				RootBone = skinnedMeshRenderer.rootBone,
				Bones = skinnedMeshRenderer.bones,
			};

			SkinId skinId;
			if (_skinCache.TryGetValue(skin, out skinId))
			{
				//already exported this skin, return it
				return skinId;
			}

			skinId = new SkinId
			{
				Id = _root.Skins.Count,
				Root = this._root
			};

			_root.Skins.Add(new GLTF.Schema.Skin
			{
				//references ExportData in Animation PR
				//WARNING: Unity ignores the scale associated with the parent node in bind poses. Need to incorporate this at some point
				InverseBindMatrices = this.ExportData(skin.BindPoses.Select(bindpose => GetRightHandedMatrix(bindpose))),
				Joints = skin.Bones.Select(bone => this._nodeCache[bone.transform]).ToList(),
			});

			return skinId;
		}

		//Skin struct for caching purposes
		private struct Skin
		{
			public Matrix4x4[] BindPoses;
			public Transform RootBone;
			public Transform[] Bones;

			public override bool Equals(object obj)
			{
				var skin = (Skin)obj;
				return
					this.BindPoses.SequenceEqual(skin.BindPoses) &&
					this.RootBone == skin.RootBone &&
					this.Bones.SequenceEqual(skin.Bones);
			}

			public override int GetHashCode()
			{
				int hashCode = this.RootBone.GetHashCode();
				foreach (var bindPose in this.BindPoses)
				{
					hashCode ^= bindPose.GetHashCode();
				}
				foreach (var bone in this.Bones)
				{
					hashCode ^= bone.GetHashCode();
				}
				return hashCode;
			}
		}

		private static Matrix4x4 GetRightHandedMatrix(Matrix4x4 matrix)
		{
			return InvertZMatrixInverse * matrix * InvertZMatrix;
		}		
	}
}
