using System;
using System.Threading;
using System.Threading.Tasks;
using GLTF;
using GLTF.Schema;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Extensions;
using Matrix4x4 = GLTF.Math.Matrix4x4;
using Vector4 = UnityEngine.Vector4;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{

		protected virtual async Task SetupBones(Skin skin, SkinnedMeshRenderer renderer, CancellationToken cancellationToken)
		{
			var boneCount = skin.Joints.Count;
			Transform[] bones = new Transform[boneCount];

			// TODO: build bindpose arrays only once per skin, instead of once per node
			float4x4[] gltfBindPoses = null;
			if (skin.InverseBindMatrices != null)
			{
				var bufferId = skin.InverseBindMatrices.Value.BufferView.Value.Buffer;
				var bufferData = await GetBufferData(bufferId);
				AttributeAccessor attributeAccessor = new AttributeAccessor
				{
					AccessorId = skin.InverseBindMatrices,
					bufferData = bufferData.bufferData,
					Offset = bufferData.ChunkOffset
				};

				GLTFHelpers.BuildBindPoseSamplers(ref attributeAccessor);
				gltfBindPoses = attributeAccessor.AccessorContent.AsMatrix4x4s;
			}

			UnityEngine.Matrix4x4[] bindPoses = new UnityEngine.Matrix4x4[boneCount];
			for (int i = 0; i < boneCount; i++)
			{
				var node = await GetNode(skin.Joints[i].Id, cancellationToken);

				bones[i] = node.transform;
				bindPoses[i] = gltfBindPoses != null ? gltfBindPoses[i].ToUnityMatrix4x4Convert() : UnityEngine.Matrix4x4.identity;
			}

			if (skin.Skeleton != null)
			{
				var rootBoneNode = await GetNode(skin.Skeleton.Id, cancellationToken);
				renderer.rootBone = rootBoneNode.transform;
			}
			else
			{
				var rootBoneId = GLTFHelpers.FindCommonAncestor(skin.Joints);
				if (rootBoneId != null)
				{
					var rootBoneNode = await GetNode(rootBoneId.Id, cancellationToken);
					renderer.rootBone = rootBoneNode.transform;
				}
				else
				{
					throw new Exception($"glTF skin joints do not share a root node! (File: {_gltfFileName})");
				}
			}
			renderer.sharedMesh.bindposes = bindPoses;
			renderer.bones = bones;
		}

		private void CreateBoneWeightArray(Vector4[] joints, Vector4[] weights, ref BoneWeight[] destArr, uint offset = 0)
		{
			// normalize weights (built-in normalize function only normalizes three components)
			for (int i = 0; i < weights.Length; i++)
			{
				var weightSum = (weights[i].x + weights[i].y + weights[i].z + weights[i].w);

				if (!Mathf.Approximately(weightSum, 0))
				{
					weights[i] /= weightSum;
				}
			}

			for (int i = 0; i < joints.Length; i++)
			{
				destArr[offset + i].boneIndex0 = (int)joints[i].x;
				destArr[offset + i].boneIndex1 = (int)joints[i].y;
				destArr[offset + i].boneIndex2 = (int)joints[i].z;
				destArr[offset + i].boneIndex3 = (int)joints[i].w;

				destArr[offset + i].weight0 = weights[i].x;
				destArr[offset + i].weight1 = weights[i].y;
				destArr[offset + i].weight2 = weights[i].z;
				destArr[offset + i].weight3 = weights[i].w;
			}
		}

	}
}
