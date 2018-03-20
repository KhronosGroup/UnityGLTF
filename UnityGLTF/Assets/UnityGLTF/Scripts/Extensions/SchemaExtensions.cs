using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Extensions
{
	public static class SchemaExtensions
	{
		public static void GetUnityTRSProperties(this Node node, out Vector3 position, out Quaternion rotation,
			out Vector3 scale)
		{
			Vector3 localPosition, localScale;
			Quaternion localRotation;

			if (!node.UseTRS)
			{
				GetTRSProperties(node.Matrix, out localPosition, out localRotation, out localScale);
			}
			else
			{
				localPosition = node.Translation.ToUnityVector3();
				localRotation = node.Rotation.ToUnityQuaternion();
				localScale = node.Scale.ToUnityVector3();
			}

			position = localPosition.switchHandedness();
			rotation = localRotation.switchHandedness();
			scale = new Vector3(localScale.x, localScale.y, localScale.z);
		}

		public static void SetUnityTransform(this Node node, Transform transform, bool useLocal=true)
		{
			Vector3 position = useLocal ? transform.localPosition : transform.position;
			node.Translation = new GLTF.Math.Vector3(position.x, position.y, -position.z);

			Quaternion rotation = useLocal ? transform.localRotation : transform.rotation;
			node.Rotation = new GLTF.Math.Quaternion(rotation.x, rotation.y, -rotation.z, -rotation.w);

			Vector3 scale = useLocal ? transform.localScale : transform.lossyScale;
			node.Scale = new GLTF.Math.Vector3(scale.x, scale.y, scale.z);
		}

		// todo: move to utility class
		public static void GetTRSProperties(GLTF.Math.Matrix4x4 mat, out Vector3 position, out Quaternion rotation,
			out Vector3 scale)
		{
			position = mat.GetColumnV3(3);

			scale = new Vector3(
				mat.GetColumnV3(0).magnitude,
				mat.GetColumnV3(1).magnitude,
				mat.GetColumnV3(2).magnitude
			);

			rotation = Quaternion.LookRotation(mat.GetColumnV3(2), mat.GetColumnV3(1));
		}

#if false
		public static SamplerId GetSamplerId(this GLTFRoot root, UnityEngine.Texture textureObj)
		{
			for (var i = 0; i < root.Samplers.Count; i++)
			{
				bool filterIsNearest = root.Samplers[i].MinFilter == MinFilterMode.Nearest
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapNearest
					|| root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapNearest;

				bool filterIsLinear = root.Samplers[i].MinFilter == MinFilterMode.Linear
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapLinear;

				bool filterMatched = textureObj.filterMode == FilterMode.Point && filterIsNearest
					|| textureObj.filterMode == FilterMode.Bilinear && filterIsLinear
					|| textureObj.filterMode == FilterMode.Trilinear && root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapLinear;

				bool wrapMatched =
textureObj.wrapMode == TextureWrapMode.Clamp && root.Samplers[i].WrapS == GLTFSerialization.WrapMode.ClampToEdge
					|| textureObj.wrapMode == TextureWrapMode.Repeat && root.Samplers[i].WrapS != GLTFSerialization.WrapMode.ClampToEdge;

				if(filterMatched && wrapMatched)
				{
					return new SamplerId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		//todo blgross unity
		public static ImageId GetImageId(this GLTFRoot root, UnityEngine.Texture textureObj)
		{
			for (var i = 0; i < Images.Count; i++)
			{
				if (Images[i].Contents == textureObj)
				{
					return new ImageId
					{
						Id = i,
						Root = this
					};
				}
			}

			return null;
		}
#endif
		public static Vector4 GetColumn(this GLTF.Math.Matrix4x4 mat, uint columnNum)
		{
			switch (columnNum)
			{
				case 0:
				{
					return new Vector4(mat.M11, mat.M12, mat.M13, mat.M14);
				}
				case 1:
				{
					return new Vector4(mat.M21, mat.M22, mat.M23, mat.M24);
				}
				case 2:
				{
					return new Vector4(mat.M31, mat.M32, mat.M33, mat.M34);
				}
				case 3:
				{
					return new Vector4(mat.M41, mat.M42, mat.M43, mat.M44);
				}
				default:
					throw new System.Exception("column num is out of bounds");
			}
		}

		public static Vector3 GetColumnV3(this GLTF.Math.Matrix4x4 mat, uint columnNum)
		{
			switch (columnNum)
			{
				case 0:
				{
					return new Vector3(mat.M11, mat.M21, mat.M31);
				}
				case 1:
				{
					return new Vector3(mat.M12, mat.M22, mat.M32);
				}
				case 2:
				{
					return new Vector3(mat.M13, mat.M23, mat.M33);
				}
				case 3:
				{
					return new Vector3(mat.M14, mat.M24, mat.M34);
				}
				default:
					throw new System.Exception("column num is out of bounds");
			}
		}

		public static Vector2 ToUnityVector2(this GLTF.Math.Vector2 vec3)
		{
			return new Vector2(vec3.X, vec3.Y);
		}

		public static Vector2[] ToUnityVector2(this GLTF.Math.Vector2[] inVecArr)
		{
			Vector2[] outVecArr = new Vector2[inVecArr.Length];
			for (int i = 0; i < inVecArr.Length; ++i)
			{
				outVecArr[i] = inVecArr[i].ToUnityVector2();
			}
			return outVecArr;
		}

		public static Vector3 ToUnityVector3(this GLTF.Math.Vector3 vec3)
		{
			return new Vector3(vec3.X, vec3.Y, vec3.Z);
		}

		public static Vector3[] ToUnityVector3(this GLTF.Math.Vector3[] inVecArr, bool switchHandedness=false)
		{
			Vector3[] outVecArr = new Vector3[inVecArr.Length];
			for (int i = 0; i < inVecArr.Length; ++i)
			{
				outVecArr[i] = inVecArr[i].ToUnityVector3();
				if (switchHandedness)
					outVecArr[i] = outVecArr[i].switchHandedness();
			}
			return outVecArr;
		}

		public static GLTF.Math.Vector4 ToGLTFVector4(this Vector4 vec4)
		{
			return new GLTF.Math.Vector4(vec4.x, vec4.y, vec4.z, vec4.w);
		}

		public static Vector4 ToUnityVector4(this GLTF.Math.Vector4 vec4)
		{
			return new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
		}

		public static Vector4[] ToUnityVector4(this GLTF.Math.Vector4[] inVecArr, bool switchHandedness = false)
		{
			Vector4[] outVecArr = new Vector4[inVecArr.Length];
			for (int i = 0; i < inVecArr.Length; ++i)
			{
				outVecArr[i] = inVecArr[i].ToUnityVector4();
				if (switchHandedness)
					outVecArr[i] = outVecArr[i].switchHandedness();
			}
			return outVecArr;
		}

		public static UnityEngine.Color ToUnityColor(this GLTF.Math.Color color)
		{
			return new UnityEngine.Color(color.R, color.G, color.B, color.A);
		}

		public static GLTF.Math.Color ToNumericsColor(this UnityEngine.Color color)
		{
			return new GLTF.Math.Color(color.r, color.g, color.b, color.a);
		}

		public static UnityEngine.Color[] ToUnityColor(this GLTF.Math.Color[] inColorArr)
		{
			UnityEngine.Color[] outColorArr = new UnityEngine.Color[inColorArr.Length];
			for (int i = 0; i < inColorArr.Length; ++i)
			{
				outColorArr[i] = inColorArr[i].ToUnityColor();
			}
			return outColorArr;
		}

		public static Quaternion ToUnityQuaternion(this GLTF.Math.Quaternion quaternion)
		{
			return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
		}

		public static Matrix4x4 ToUnityMatrix(this GLTF.Math.Matrix4x4 matrix)
		{
			Matrix4x4 mat = new Matrix4x4();
			mat.SetColumn(0, matrix.GetColumn(0));
			mat.SetColumn(1, matrix.GetColumn(1));
			mat.SetColumn(2, matrix.GetColumn(2));
			mat.SetColumn(3, matrix.GetColumn(3));
			return mat;
		}

		public static GLTF.Math.Matrix4x4 ToGLTFMAtrix(this Matrix4x4 matrix)
		{
			return new GLTF.Math.Matrix4x4(
				matrix.GetColumn(0).ToGLTFVector4(),
				matrix.GetColumn(1).ToGLTFVector4(),
				matrix.GetColumn(2).ToGLTFVector4(),
				matrix.GetColumn(3).ToGLTFVector4()
			);
		}

		public static Vector3 switchHandedness(this Vector3 input)
		{
			return new Vector3(input.x, input.y, -input.z);
		}

		public static Vector4 switchHandedness(this Vector4 input)
		{
			return new Vector4(input.x, input.y, -input.z, -input.w);
		}


		public static Quaternion switchHandedness(this Quaternion input)
		{
			return new Quaternion(input.x, input.y, -input.z, -input.w);
		}

		public static Matrix4x4 switchHandedness(this Matrix4x4 matrix)
		{
			Vector3 position = matrix.GetColumn(3).switchHandedness();
			Quaternion rotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1)).switchHandedness();
			Vector3 scale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

			float epsilon = 0.00001f;

			// Some issues can occurs with non uniform scales
			if (Mathf.Abs(scale.x - scale.y) > epsilon || Mathf.Abs(scale.y - scale.z) > epsilon || Mathf.Abs(scale.x - scale.z) > epsilon)
			{
				Debug.LogWarning("A matrix with non uniform scale is being converted from left to right handed system. This code is not working correctly in this case");
			}

			// Handle negative scale component in matrix decomposition
			if (Matrix4x4.Determinant(matrix) < 0)
			{
				Quaternion rot = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
				Matrix4x4 corr = Matrix4x4.TRS(matrix.GetColumn(3), rot, Vector3.one).inverse;
				Matrix4x4 extractedScale = corr * matrix;
				scale = new Vector3(extractedScale.m00, extractedScale.m11, extractedScale.m22);
			}

			// convert transform values from left handed to right handed
			return Matrix4x4.TRS(position, rotation, scale);
		}
	}
}
