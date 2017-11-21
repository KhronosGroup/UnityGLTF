using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Extensions
{
	public static class SchemaExtensions
	{
        /// <summary>
        /// Get the converted unity translation, rotation, and scale from a gltf node
        /// </summary>
        /// <param name="node">gltf node</param>
        /// <param name="position">unity translation vector</param>
        /// <param name="rotation">unity rotation quaternion</param>
        /// <param name="scale">unity scale vector</param>
		public static void GetUnityTRSProperties(this Node node, out Vector3 position, out Quaternion rotation,
			out Vector3 scale)
		{
			if (!node.UseTRS)
			{
                Matrix4x4 unityMat = node.Matrix.ToUnityMatrix4x4Convert();
                unityMat.GetTRSProperties(out position, out rotation, out scale);
            }
			else
			{
				position = node.Translation.ToUnityVector3Convert();
				rotation = node.Rotation.ToUnityQuaternionConvert();
				scale = node.Scale.ToUnityVector3Raw();
			}
		}

        /// <summary>
        /// Set a gltf node's converted translation, rotation, and scale from a unity transform
        /// </summary>
        /// <param name="node">gltf node to modify</param>
        /// <param name="transform">unity transform to convert</param>
		public static void SetUnityTransform(this Node node, Transform transform)
		{
            node.Translation = transform.localPosition.ToGltfVector3Convert();
            node.Rotation = transform.localRotation.ToGltfQuaternionConvert();
            node.Scale = transform.localScale.ToGltfVector3Raw();
        }

		// todo: move to utility class
        /// <summary>
        /// Get unity translation, rotation, and scale from a unity matrix
        /// </summary>
        /// <param name="mat">unity matrix to get properties from</param>
        /// <param name="position">unity translation vector</param>
        /// <param name="rotation">unity rotation quaternion</param>
        /// <param name="scale">unity scale vector</param>
		public static void GetTRSProperties(this Matrix4x4 mat, out Vector3 position, out Quaternion rotation,
			out Vector3 scale)
		{
			position = mat.GetColumn(3);

            Vector3 x = mat.GetColumn(0);
            Vector3 y = mat.GetColumn(1);
            Vector3 z = mat.GetColumn(2);
            Vector3 calculatedZ = Vector3.Cross(x, y);
            bool mirrored = Vector3.Dot(calculatedZ, z) < 0.0f;

            scale = new Vector3(x.magnitude * (mirrored ? -1.0f : 1.0f), y.magnitude, z.magnitude);

			rotation = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
		}

        /// <summary>
        /// Get converted unity translation, rotation, and scale from a gltf matrix
        /// </summary>
        /// <param name="gltfMat">gltf matrix to get and convert properties from</param>
        /// <param name="position">unity translation vector</param>
        /// <param name="rotation">unity rotation quaternion</param>
        /// <param name="scale">unity scale vector</param>
        public static void GetTRSProperties(this GLTF.Math.Matrix4x4 gltfMat, out Vector3 position, out Quaternion rotation,
            out Vector3 scale)
        {
            gltfMat.ToUnityMatrix4x4Convert().GetTRSProperties(out position, out rotation, out scale);
        }

        /// <summary>
        /// Get a gltf column vector from a gltf matrix
        /// </summary>
        /// <param name="mat">gltf matrix</param>
        /// <param name="columnNum">the specified column vector from the matrix</param>
        /// <returns></returns>
        public static GLTF.Math.Vector4 GetColumn(this GLTF.Math.Matrix4x4 mat, uint columnNum)
		{
			switch(columnNum)
			{
				case 0:
				{
					return new GLTF.Math.Vector4(mat.M11, mat.M21, mat.M31, mat.M41);
				}
				case 1:
				{
					return new GLTF.Math.Vector4(mat.M12, mat.M22, mat.M32, mat.M42);
				}
				case 2:
				{
					return new GLTF.Math.Vector4(mat.M13, mat.M23, mat.M33, mat.M43);
				}
				case 3:
				{
					return new GLTF.Math.Vector4(mat.M14, mat.M24, mat.M34, mat.M44);
				}
				default:
					throw new System.Exception("column num is out of bounds");
			}
		}

        /// <summary>
        /// Convert gltf quaternion to a unity quaternion
        /// </summary>
        /// <param name="gltfQuat">gltf quaternion</param>
        /// <returns>unity quaternion</returns>
        public static Quaternion ToUnityQuaternionConvert(this GLTF.Math.Quaternion gltfQuat)
        {
            // get raw matrix conversion (gltf matrix stored in a unity matrix for easier math)
            Quaternion rawUnityQuat = gltfQuat.ToUnityQuaternionRaw();
            Matrix4x4 rawUnityMat = Matrix4x4.TRS(Vector3.zero, rawUnityQuat, Vector3.one);

            // convert matrix coordinate space
            Vector3 coordinateSpaceConversionScale = GLTFSceneImporter.CoordinateSpaceConversionScale.ToUnityVector3Raw();
            Matrix4x4 convert = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, coordinateSpaceConversionScale);
            Matrix4x4 unityConvertedMat = convert * rawUnityMat * convert;

            // get quaternion from matrix
            Quaternion unityConvertedQuat = Quaternion.LookRotation(unityConvertedMat.GetColumn(2), unityConvertedMat.GetColumn(1));
            return unityConvertedQuat;
        }

        /// <summary>
        /// Convert unity quaternion to a gltf quaternion
        /// </summary>
        /// <param name="unityQuat">unity quaternion</param>
        /// <returns>gltf quaternion</returns>
        public static GLTF.Math.Quaternion ToGltfQuaternionConvert(this Quaternion unityQuat)
        {
            // get unity matrix from quaternion
            Matrix4x4 unityMat = Matrix4x4.TRS(Vector3.zero, unityQuat, Vector3.one);

            // convert matrix coordinate space
            Vector3 coordinateSpaceConversionScale = GLTFSceneImporter.CoordinateSpaceConversionScale.ToUnityVector3Raw();
            Matrix4x4 convert = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, coordinateSpaceConversionScale);
            Matrix4x4 unityConvertedMat = convert * unityMat * convert;

            // get quaternion from matrix
            Quaternion unityConvertedQuat = Quaternion.LookRotation(unityConvertedMat.GetColumn(2), unityConvertedMat.GetColumn(1));
            return unityConvertedQuat.ToGltfQuaternionRaw();
        }

        /// <summary>
        /// Convert gltf matrix to a unity matrix
        /// </summary>
        /// <param name="gltfMat">gltf matrix</param>
        /// <returns>unity matrix</returns>
        public static Matrix4x4 ToUnityMatrix4x4Convert(this GLTF.Math.Matrix4x4 gltfMat)
        {
            Matrix4x4 rawUnityMat = gltfMat.ToUnityMatrix4x4Raw();
            Vector3 coordinateSpaceConversionScale = GLTFSceneImporter.CoordinateSpaceConversionScale.ToUnityVector3Raw();
            Matrix4x4 convert = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, coordinateSpaceConversionScale);
            Matrix4x4 unityMat = convert * rawUnityMat * convert;
            return unityMat;
        }

        /// <summary>
        /// Convert gltf Vector3 to unity Vector3
        /// </summary>
        /// <param name="gltfVec3">gltf vector3</param>
        /// <returns>unity vector3</returns>
        public static Vector3 ToUnityVector3Convert(this GLTF.Math.Vector3 gltfVec3)
        {
            Vector3 coordinateSpaceConversionScale = GLTFSceneImporter.CoordinateSpaceConversionScale.ToUnityVector3Raw();
            Vector3 unityVec3 = Vector3.Scale(gltfVec3.ToUnityVector3Raw(), coordinateSpaceConversionScale);
            return unityVec3;
        }

        /// <summary>
        /// Convert unity Vector3 to gltf Vector3
        /// </summary>
        /// <param name="unityVec3">unity Vector3</param>
        /// <returns>gltf Vector3</returns>
        public static GLTF.Math.Vector3 ToGltfVector3Convert(this Vector3 unityVec3)
        {
            Vector3 coordinateSpaceConversionScale = GLTFSceneImporter.CoordinateSpaceConversionScale.ToUnityVector3Raw();
            GLTF.Math.Vector3 gltfVec3 = Vector3.Scale(unityVec3, coordinateSpaceConversionScale).ToGltfVector3Raw();
            return gltfVec3;
        }

        public static GLTF.Math.Vector3 ToGltfVector3Raw(this Vector3 unityVec3)
        {
            GLTF.Math.Vector3 gltfVec3 = new GLTF.Math.Vector3(unityVec3.x, unityVec3.y, unityVec3.z);
            return gltfVec3;
        }

        public static Matrix4x4 ToUnityMatrix4x4Raw(this GLTF.Math.Matrix4x4 gltfMat)
        {
            Vector4 rawUnityCol0 = gltfMat.GetColumn(0).ToUnityVector4Raw();
            Vector4 rawUnityCol1 = gltfMat.GetColumn(1).ToUnityVector4Raw();
            Vector4 rawUnityCol2 = gltfMat.GetColumn(2).ToUnityVector4Raw();
            Vector4 rawUnityCol3 = gltfMat.GetColumn(3).ToUnityVector4Raw();
            Matrix4x4 rawUnityMat = new UnityEngine.Matrix4x4(rawUnityCol0, rawUnityCol1, rawUnityCol2, rawUnityCol3);
            return rawUnityMat;
        }

        public static Vector2 ToUnityVector2Raw(this GLTF.Math.Vector2 vec2)
		{
			return new Vector2(vec2.X, vec2.Y);
		}

		public static Vector2[] ToUnityVector2Raw(this GLTF.Math.Vector2[] inVecArr)
		{
			Vector2[] outVecArr = new Vector2[inVecArr.Length];
			for (int i = 0; i < inVecArr.Length; ++i)
			{
				outVecArr[i] = inVecArr[i].ToUnityVector2Raw();
			}
			return outVecArr;
		}

		public static Vector3 ToUnityVector3Raw(this GLTF.Math.Vector3 vec3)
		{
			return new Vector3(vec3.X, vec3.Y, vec3.Z);
		}

		public static Vector3[] ToUnityVector3Raw(this GLTF.Math.Vector3[] inVecArr)
		{
			Vector3[] outVecArr = new Vector3[inVecArr.Length];
			for (int i = 0; i < inVecArr.Length; ++i)
			{
				outVecArr[i] = inVecArr[i].ToUnityVector3Raw();
			}
			return outVecArr;
		}

		public static Vector4 ToUnityVector4Raw(this GLTF.Math.Vector4 vec4)
		{
			return new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
		}

		public static Vector4[] ToUnityVector4Raw(this GLTF.Math.Vector4[] inVecArr)
		{
			Vector4[] outVecArr = new Vector4[inVecArr.Length];
			for (int i = 0; i < inVecArr.Length; ++i)
			{
				outVecArr[i] = inVecArr[i].ToUnityVector4Raw();
			}
			return outVecArr;
		}

		public static UnityEngine.Color ToUnityColorRaw(this GLTF.Math.Color color)
		{
			return new UnityEngine.Color(color.R, color.G, color.B, color.A);
		}

		public static GLTF.Math.Color ToNumericsColorRaw(this UnityEngine.Color color)
		{
			return new GLTF.Math.Color(color.r, color.g, color.b, color.a);
		}

		public static UnityEngine.Color[] ToUnityColorRaw(this GLTF.Math.Color[] inColorArr)
		{
			UnityEngine.Color[] outColorArr = new UnityEngine.Color[inColorArr.Length];
			for (int i = 0; i < inColorArr.Length; ++i)
			{
				outColorArr[i] = inColorArr[i].ToUnityColorRaw();
			}
			return outColorArr;
		}

		public static int[] ToIntArrayRaw(this uint[] uintArr)
		{
			int[] intArr = new int[uintArr.Length];
			for (int i = 0; i < uintArr.Length; ++i)
			{
				uint uintVal = uintArr[i];
				Debug.Assert(uintVal <= int.MaxValue);
				intArr[i] = (int)uintVal;
			}

			return intArr;
		}

        public static GLTF.Math.Quaternion ToGltfQuaternionRaw(this Quaternion unityQuat)
        {
            return new GLTF.Math.Quaternion(unityQuat.x, unityQuat.y, unityQuat.z, unityQuat.w);
        }

        public static Quaternion ToUnityQuaternionRaw(this GLTF.Math.Quaternion quaternion)
		{
			return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
		}
	}
}
