using GLTF;
using GLTF.Schema;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Extensions
{
	public static class SchemaExtensions
	{
		/// <summary>
		/// Define the transformation between Unity coordinate space and glTF.
		/// glTF is a right-handed coordinate system, where the 'right' direction is -X relative to
		/// Unity's coordinate system.
		/// glTF matrix: column vectors, column-major storage, +Y up, +Z forward, -X right, right-handed
		/// unity matrix: column vectors, column-major storage, +Y up, +Z forward, +X right, left-handed
		/// multiply by a negative X scale to convert handedness
		/// </summary>
		public static readonly GLTF.Math.Vector3 CoordinateSpaceConversionScale = new GLTF.Math.Vector3(-1, 1, 1);

		/// <summary>
		/// Define whether the coordinate space scale conversion above means we have a change in handedness.
		/// This is used when determining the conventional direction of rotation - the right-hand rule states
		/// that rotations are clockwise in left-handed systems and counter-clockwise in right-handed systems.
		/// Reversing the direction of one or three axes of reverses the handedness.
		/// </summary>
		public static bool CoordinateSpaceConversionRequiresHandednessFlip
		{
			get
			{
				return CoordinateSpaceConversionScale.X * CoordinateSpaceConversionScale.Y * CoordinateSpaceConversionScale.Z < 0.0f;
			}
		}

		public static readonly GLTF.Math.Vector4 TangentSpaceConversionScale = new GLTF.Math.Vector4(-1, 1, 1, -1);

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

		internal static readonly Quaternion InvertDirection = new Quaternion(0, -1, 0, 0);

		/// <summary>
		/// Set a gltf node's converted translation, rotation, and scale from a unity transform
		/// </summary>
		/// <param name="node">gltf node to modify</param>
		/// <param name="transform">unity transform to convert</param>
		/// <param name="invertLookDirection">invert look direction (e.g. for lights and cameras)</param>
		public static void SetUnityTransform(this Node node, Transform transform, bool invertLookDirection)
		{
			node.Translation = transform.localPosition.ToGltfVector3Convert();
			node.Rotation = (transform.localRotation * (invertLookDirection ? InvertDirection : Quaternion.identity)).ToGltfQuaternionConvert();
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
			switch (columnNum)
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
			Vector3 fromAxisOfRotation = new Vector3(gltfQuat.X, gltfQuat.Y, gltfQuat.Z);
			float axisFlipScale = CoordinateSpaceConversionRequiresHandednessFlip ? -1.0f : 1.0f;
			Vector3 toAxisOfRotation = axisFlipScale * Vector3.Scale(fromAxisOfRotation, CoordinateSpaceConversionScale.ToUnityVector3Raw());

			return new Quaternion(toAxisOfRotation.x, toAxisOfRotation.y, toAxisOfRotation.z, gltfQuat.W);
		}
		
		public static Quaternion ToUnityQuaternionConvert(this quaternion quat)
		{
			Vector3 fromAxisOfRotation = new Vector3(quat.value.x, quat.value.y, quat.value.z);
			float axisFlipScale = CoordinateSpaceConversionRequiresHandednessFlip ? -1.0f : 1.0f;
			Vector3 toAxisOfRotation = axisFlipScale * Vector3.Scale(fromAxisOfRotation, CoordinateSpaceConversionScale.ToUnityVector3Raw());

			return new Quaternion(toAxisOfRotation.x, toAxisOfRotation.y, toAxisOfRotation.z, quat.value.w);
		}
		
		public static Quaternion ToUnityQuaternionConvert(this float4 quat)
		{
			Vector3 fromAxisOfRotation = new Vector3(quat.x, quat.y, quat.z);
			float axisFlipScale = CoordinateSpaceConversionRequiresHandednessFlip ? -1.0f : 1.0f;
			Vector3 toAxisOfRotation = axisFlipScale * Vector3.Scale(fromAxisOfRotation, CoordinateSpaceConversionScale.ToUnityVector3Raw());

			return new Quaternion(toAxisOfRotation.x, toAxisOfRotation.y, toAxisOfRotation.z, quat.w);
		}		

		/// <summary>
		/// Convert unity quaternion to a gltf quaternion
		/// </summary>
		/// <param name="unityQuat">unity quaternion</param>
		/// <returns>gltf quaternion</returns>
		public static GLTF.Math.Quaternion ToGltfQuaternionConvert(this Quaternion unityQuat)
		{
			Vector3 fromAxisOfRotation = new Vector3(unityQuat.x, unityQuat.y, unityQuat.z);
			float axisFlipScale = CoordinateSpaceConversionRequiresHandednessFlip ? -1.0f : 1.0f;
			Vector3 toAxisOfRotation = axisFlipScale * Vector3.Scale(fromAxisOfRotation, CoordinateSpaceConversionScale.ToUnityVector3Raw());

			return new GLTF.Math.Quaternion(toAxisOfRotation.x, toAxisOfRotation.y, toAxisOfRotation.z, unityQuat.w);
		}

		/// <summary>
		/// Convert gltf matrix to a unity matrix
		/// </summary>
		/// <param name="gltfMat">gltf matrix</param>
		/// <returns>unity matrix</returns>
		public static Matrix4x4 ToUnityMatrix4x4Convert(this GLTF.Math.Matrix4x4 gltfMat)
		{
			Matrix4x4 rawUnityMat = gltfMat.ToUnityMatrix4x4Raw();
			Vector3 coordinateSpaceConversionScale = CoordinateSpaceConversionScale.ToUnityVector3Raw();
			Matrix4x4 convert = Matrix4x4.Scale(coordinateSpaceConversionScale);
			Matrix4x4 unityMat = convert * rawUnityMat * convert;
			return unityMat;
		}
		
		/// <summary>
		/// Convert gltf matrix to a unity matrix
		/// </summary>
		/// <param name="gltfMat">gltf matrix</param>
		/// <returns>unity matrix</returns>
		public static Matrix4x4 ToUnityMatrix4x4Convert(this float4x4 gltfMat)
		{
			Matrix4x4 rawUnityMat = gltfMat.ToUnityMatrix4x4Raw();
			Vector3 coordinateSpaceConversionScale = CoordinateSpaceConversionScale.ToUnityVector3Raw();
			Matrix4x4 convert = Matrix4x4.Scale(coordinateSpaceConversionScale);
			Matrix4x4 unityMat = convert * rawUnityMat * convert;
			return unityMat;
		}		

		/// <summary>
		/// Convert gltf matrix to a unity matrix
		/// </summary>
		/// <param name="unityMat">unity matrix</param>
		/// <returns>gltf matrix</returns>
		public static GLTF.Math.Matrix4x4 ToGltfMatrix4x4Convert(this Matrix4x4 unityMat)
		{
			Vector3 coordinateSpaceConversionScale = CoordinateSpaceConversionScale.ToUnityVector3Raw();
			Matrix4x4 convert = Matrix4x4.Scale(coordinateSpaceConversionScale);
			GLTF.Math.Matrix4x4 gltfMat = (convert * unityMat * convert).ToGltfMatrix4x4Raw();
			return gltfMat;
		}

		/// <summary>
		/// Convert gltf Vector3 to unity Vector3
		/// </summary>
		/// <param name="gltfVec3">gltf vector3</param>
		/// <returns>unity vector3</returns>
		public static Vector3 ToUnityVector3Convert(this GLTF.Math.Vector3 gltfVec3)
		{
			Vector3 coordinateSpaceConversionScale = CoordinateSpaceConversionScale.ToUnityVector3Raw();
			Vector3 unityVec3 = Vector3.Scale(gltfVec3.ToUnityVector3Raw(), coordinateSpaceConversionScale);
			return unityVec3;
		}

		/// <summary>
		/// Convert gltf Vector3 to unity Vector3
		/// </summary>
		/// <param name="gltfVec3">gltf vector3</param>
		/// <returns>unity vector3</returns>
		public static Vector3 ToUnityVector3Convert(this float3 gltfVec3)
		{
			Vector3 coordinateSpaceConversionScale = CoordinateSpaceConversionScale.ToUnityVector3Raw();
			Vector3 unityVec3 = Vector3.Scale(gltfVec3.ToUnityVector3Raw(), coordinateSpaceConversionScale);
			return unityVec3;
		}		

		public static float3 ToUnityFloat3Convert(this float3 gltfVec3)
		{
			float3 coordinateSpaceConversionScale = new float3(CoordinateSpaceConversionScale.X, CoordinateSpaceConversionScale.Y, CoordinateSpaceConversionScale.Z);
			float3 unityVec3 = gltfVec3 * coordinateSpaceConversionScale;
			return unityVec3;
		}		
		
				
		public static void ToUnityVector3Convert(this float3[] vec3, Vector3[] arr, int offset)
		{
			float3 conversion = new float3(CoordinateSpaceConversionScale.X,
				CoordinateSpaceConversionScale.Y,
				CoordinateSpaceConversionScale.Z);
			
			for (int i = 0; i < vec3.Length; i++)
			{
				arr[i+offset] = vec3[i] * conversion;
			}
		}			

		
		/// <summary>
		/// Convert unity Vector3 to gltf Vector3
		/// </summary>
		/// <param name="unityVec3">unity Vector3</param>
		/// <returns>gltf Vector3</returns>
		public static GLTF.Math.Vector3 ToGltfVector3Convert(this Vector3 unityVec3)
		{
			Vector3 coordinateSpaceConversionScale = CoordinateSpaceConversionScale.ToUnityVector3Raw();
			GLTF.Math.Vector3 gltfVec3 = Vector3.Scale(unityVec3, coordinateSpaceConversionScale).ToGltfVector3Raw();
			return gltfVec3;
		}

		public static GLTF.Math.Vector3 ToGltfVector3Raw(this Vector3 unityVec3)
		{
			GLTF.Math.Vector3 gltfVec3 = new GLTF.Math.Vector3(unityVec3.x, unityVec3.y, unityVec3.z);
			return gltfVec3;
		}
		
		public static GLTF.Math.Vector4 ToGltfVector4Raw(this Vector4 unityVec4)
		{
			GLTF.Math.Vector4 gltfVec4 = new GLTF.Math.Vector4(unityVec4.x, unityVec4.y, unityVec4.z, unityVec4.w);
			return gltfVec4;
		}

		public static Matrix4x4 ToUnityMatrix4x4Raw(this GLTF.Math.Matrix4x4 gltfMat)
		{
			Vector4 rawUnityCol0 = gltfMat.GetColumn(0).ToUnityVector4Raw();
			Vector4 rawUnityCol1 = gltfMat.GetColumn(1).ToUnityVector4Raw();
			Vector4 rawUnityCol2 = gltfMat.GetColumn(2).ToUnityVector4Raw();
			Vector4 rawUnityCol3 = gltfMat.GetColumn(3).ToUnityVector4Raw();
			Matrix4x4 rawUnityMat = new UnityEngine.Matrix4x4();
			rawUnityMat.SetColumn(0, rawUnityCol0);
			rawUnityMat.SetColumn(1, rawUnityCol1);
			rawUnityMat.SetColumn(2, rawUnityCol2);
			rawUnityMat.SetColumn(3, rawUnityCol3);

			return rawUnityMat;
		}
		
		public static Matrix4x4 ToUnityMatrix4x4Raw(this float4x4 gltfMat)
		{
			Vector4 rawUnityCol0 = gltfMat.c0.ToUnityVector4Raw();
			Vector4 rawUnityCol1 = gltfMat.c1.ToUnityVector4Raw();
			Vector4 rawUnityCol2 = gltfMat.c2.ToUnityVector4Raw();
			Vector4 rawUnityCol3 = gltfMat.c3.ToUnityVector4Raw();
			Matrix4x4 rawUnityMat = new UnityEngine.Matrix4x4();
			rawUnityMat.SetColumn(0, rawUnityCol0);
			rawUnityMat.SetColumn(1, rawUnityCol1);
			rawUnityMat.SetColumn(2, rawUnityCol2);
			rawUnityMat.SetColumn(3, rawUnityCol3);

			return rawUnityMat;
		}		

		public static GLTF.Math.Matrix4x4 ToGltfMatrix4x4Raw(this Matrix4x4 unityMat)
		{
			GLTF.Math.Vector4 c0 = unityMat.GetColumn(0).ToGltfVector4Raw();
			GLTF.Math.Vector4 c1 = unityMat.GetColumn(1).ToGltfVector4Raw();
			GLTF.Math.Vector4 c2 = unityMat.GetColumn(2).ToGltfVector4Raw();
			GLTF.Math.Vector4 c3 = unityMat.GetColumn(3).ToGltfVector4Raw();
			GLTF.Math.Matrix4x4 rawGltfMat = new GLTF.Math.Matrix4x4(c0.X, c0.Y, c0.Z, c0.W, c1.X, c1.Y, c1.Z, c1.W, c2.X, c2.Y, c2.Z, c2.W, c3.X, c3.Y, c3.Z, c3.W);
			return rawGltfMat;
		}

		public static Vector2 ToUnityVector2Raw(this GLTF.Math.Vector2 vec2)
		{
			return new Vector2(vec2.X, vec2.Y);
		}
		
		public static Vector2 ToUnityVector2Raw(this float2 vec2)
		{
			return new Vector2(vec2.x, vec2.y);
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
		
		public static Vector2[] ToUnityVector2Raw(this float2[] vec2)
		{
			var r = new Vector2[vec2.Length];
			for (int i = 0; i < vec2.Length; i++)
			{
				r[i] = vec2[i].ToUnityVector2Raw();
			}

			return r;
		}	

		public static void ToUnityVector2Raw(this float2[] vec2, Vector2[] arr, int offset)
		{
			for (int i = 0; i < vec2.Length; i++)
			{
				arr[i+offset] = vec2[i].ToUnityVector2Raw();
			}
		}
		
		public static Vector3 ToUnityVector3Raw(this GLTF.Math.Vector3 vec3)
		{
			return new Vector3(vec3.X, vec3.Y, vec3.Z);
		}

		public static Vector3 ToUnityVector3Raw(this float3 vec3)
		{
			return new Vector3(vec3.x, vec3.y, vec3.z);
		}
		
		public static Vector3[] ToUnityVector3Raw(this float3[] vec3)
		{
			var r = new Vector3[vec3.Length];
			for (int i = 0; i < vec3.Length; i++)
			{
				r[i] = vec3[i].ToUnityVector3Raw();
			}

			return r;
		}	
		
		public static void ToUnityVector3Raw(this float3[] vec3, Vector3[] arr, int offset)
		{
			for (int i = 0; i < vec3.Length; i++)
			{
				arr[i+offset] = vec3[i].ToUnityVector3Raw();
			}
		}		
		
		public static Vector4 ToUnityVector4Raw(this GLTF.Math.Vector4 vec4)
		{
			return new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
		}

		public static Vector4 ToUnityVector4Raw(this float4 vec4)
		{
			return new Vector4(vec4.x, vec4.y, vec4.z, vec4.w);
		}		
		
		public static Vector4[] ToUnityVector4Raw(this float4[] vec4)
		{
			var r = new Vector4[vec4.Length];
			for (int i = 0; i < vec4.Length; i++)
			{
				r[i] = vec4[i].ToUnityVector4Raw();
			}

			return r;
		}	
		
		public static void ToUnityVector4Raw(this float4[] vec4, Vector4[] arr, int offset)
		{
			for (int i = 0; i < vec4.Length; i++)
			{
				arr[i+offset] = vec4[i].ToUnityVector4Raw();
			}
		}			

		public static UnityEngine.Color ToUnityColorRaw(this GLTF.Math.Color color)
		{
			var c = new UnityEngine.Color(color.R, color.G, color.B, color.A).gamma;
			return c;
		}

		public static void ToUnityColorRaw(this float4[] inArr, Color[] outArr, int offset)
		{
			for (int i = 0; i < inArr.Length; i++)
			{
				outArr[i + offset] = inArr[i].ToUnityColorRaw();
			}
		}
		
		public static UnityEngine.Color ToUnityColorRaw(this float4 color)
		{
			var c = new UnityEngine.Color(color.x, color.y, color.z, color.w).gamma;
			return c;
		}

		public static UnityEngine.Color ToUnityColorLinear(this GLTF.Math.Color color)
		{
			var c = new UnityEngine.Color(color.R, color.G, color.B, color.A);
			return c;
		}

		public static void ToUnityColorLinear(this float4[] inArr, Color[] outArr, int offset)
		{
			for (int i = 0; i < inArr.Length; i++)
			{
				outArr[i + offset] = inArr[i].ToUnityColorLinear();
			}
		}
		
		public static UnityEngine.Color ToUnityColorLinear(this float4 color)
		{
			var c = new UnityEngine.Color(color.x, color.y, color.z, color.w);
			return c;
		}
		
		public static UnityEngine.Color ToUnityColorGamma(this GLTF.Math.Color color)
		{
			var c = new UnityEngine.Color(color.R, color.G, color.B, color.A).linear;
			return c;
		}

		public static GLTF.Math.Color ToNumericsColorRaw(this UnityEngine.Color color)
		{
			var c = color;
			return new GLTF.Math.Color(c.r, c.g, c.b, c.a);
		}

		public static GLTF.Math.Color ToNumericsColorLinear(this UnityEngine.Color color)
		{
			var lc = color.linear;
			return new GLTF.Math.Color(lc.r, lc.g, lc.b, lc.a);
		}
		
		public static GLTF.Math.Color ToNumericsColorGamma(this UnityEngine.Color color)
		{
			var lc = color.gamma;
			return new GLTF.Math.Color(lc.r, lc.g, lc.b, lc.a);
		}

		public static GLTF.Math.Color[] ToNumericsColorLinear(this UnityEngine.Color[] inColorArr)
		{
			GLTF.Math.Color[] outColorArr = new GLTF.Math.Color[inColorArr.Length];
			for (int i = 0; i < inColorArr.Length; ++i)
			{
				outColorArr[i] = inColorArr[i].ToNumericsColorLinear();
			}
			return outColorArr;
		}

		public static UnityEngine.Color[] ToLinear(this UnityEngine.Color[] inColorArr)
		{
			UnityEngine.Color[] outColorArr = new UnityEngine.Color[inColorArr.Length];
			for (int i = 0; i < inColorArr.Length; ++i)
			{
				outColorArr[i] = inColorArr[i].linear;
			}
			return outColorArr;
		}

		public static void ToUnityColorRaw(this GLTF.Math.Color[] inArr, Color[] outArr, int offset = 0)
		{
			for (int i = 0; i < inArr.Length; i++)
			{
				outArr[offset + i] = inArr[i].ToUnityColorRaw();
			}
		}

		public static void ToUnityColorLinear(this GLTF.Math.Color[] inArr, Color[] outArr, int offset = 0)
		{
			for (int i = 0; i < inArr.Length; i++)
			{
				outArr[offset + i] = inArr[i].ToUnityColorLinear();
			}
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

		public static Quaternion ToUnityQuaternionRaw(this quaternion quaternion)
		{
			return new Quaternion(quaternion.value.x, quaternion.value.y, quaternion.value.z, quaternion.value.w);
		}
		
		public static Quaternion ToUnityQuaternionRaw(this float4 quaternion)
		{
			return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
		}		

		/// <summary>
		/// Flips the V component of the UV (1-V) to put from glTF into Unity space
		/// </summary>
		/// <param name="attributeAccessor">The attribute accessor to modify</param>
		public static void FlipTexCoordArrayV(ref AttributeAccessor attributeAccessor)
		{
			for (var i = 0; i < attributeAccessor.AccessorContent.AsFloat2s.Length; i++)
			{
				attributeAccessor.AccessorContent.AsFloat2s[i].y = 1.0f - attributeAccessor.AccessorContent.AsFloat2s[i].y;
			}
		}

		/// <summary>
		/// Flip the V component of the UV (1-V)
		/// </summary>
		/// <param name="array">The array to copy from and modify</param>
		/// <returns>Copied Vector2 with coordinates in glTF space</returns>
		public static UnityEngine.Vector2[] FlipTexCoordArrayVAndCopy(UnityEngine.Vector2[] array)
		{
			var returnArray = new UnityEngine.Vector2[array.Length];

			for (var i = 0; i < array.Length; i++)
			{
				returnArray[i].x = array[i].x;
				returnArray[i].y = 1.0f - array[i].y;
			}

			return returnArray;
		}

		/// <summary>
		/// Converts vector3 to specified coordinate space
		/// </summary>
		/// <param name="attributeAccessor">The attribute accessor to modify</param>
		/// <param name="coordinateSpaceCoordinateScale">The coordinate space to move into</param>
		public static void ConvertVector3CoordinateSpace(ref AttributeAccessor attributeAccessor, GLTF.Math.Vector3 coordinateSpaceCoordinateScale)
		{
			for (int i = 0; i < attributeAccessor.AccessorContent.AsFloat3s.Length; i++)
			{
				attributeAccessor.AccessorContent.AsFloat3s[i].x *= coordinateSpaceCoordinateScale.X;
				attributeAccessor.AccessorContent.AsFloat3s[i].y *= coordinateSpaceCoordinateScale.Y;
				attributeAccessor.AccessorContent.AsFloat3s[i].z *= coordinateSpaceCoordinateScale.Z;
			}
		}

		/// <summary>
		/// Converts and copies based on the specified coordinate scale
		/// </summary>
		/// <param name="array">The array to convert and copy</param>
		/// <param name="coordinateSpaceCoordinateScale">The specified coordinate space</param>
		/// <returns>The copied and converted coordinate space</returns>
		public static UnityEngine.Vector3[] ConvertVector3CoordinateSpaceAndCopy(Vector3[] array, GLTF.Math.Vector3 coordinateSpaceCoordinateScale)
		{
			var returnArray = new UnityEngine.Vector3[array.Length];
			var coordinateScale = coordinateSpaceCoordinateScale.ToUnityVector3Raw();

			for (int i = 0; i < array.Length; i++)
			{
				returnArray[i] = array[i];
				returnArray[i].Scale(coordinateScale);
			}

			return returnArray;
		}

		/// <summary>
		/// Converts vector4 to specified coordinate space
		/// </summary>
		/// <param name="attributeAccessor">The attribute accessor to modify</param>
		/// <param name="coordinateSpaceCoordinateScale">The coordinate space to move into</param>
		public static void ConvertVector4CoordinateSpace(ref AttributeAccessor attributeAccessor, GLTF.Math.Vector4 coordinateSpaceCoordinateScale)
		{
			for (int i = 0; i < attributeAccessor.AccessorContent.AsFloat4s.Length; i++)
			{
				attributeAccessor.AccessorContent.AsFloat4s[i].x *= coordinateSpaceCoordinateScale.X;
				attributeAccessor.AccessorContent.AsFloat4s[i].y *= coordinateSpaceCoordinateScale.Y;
				attributeAccessor.AccessorContent.AsFloat4s[i].z *= coordinateSpaceCoordinateScale.Z;
				attributeAccessor.AccessorContent.AsFloat4s[i].w *= coordinateSpaceCoordinateScale.W;
			}
		}

		/// <summary>
		/// Converts and copies based on the specified coordinate scale
		/// </summary>
		/// <param name="array">The array to convert and copy</param>
		/// <param name="coordinateSpaceCoordinateScale">The specified coordinate space</param>
		/// <returns>The copied and converted coordinate space</returns>
		public static Vector4[] ConvertVector4CoordinateSpaceAndCopy(Vector4[] array, GLTF.Math.Vector4 coordinateSpaceCoordinateScale)
		{
			var returnArray = new Vector4[array.Length];
			var coordinateScale = coordinateSpaceCoordinateScale.ToUnityVector4Raw();

			for (var i = 0; i < array.Length; i++)
			{
				returnArray[i] = array[i];
				returnArray[i].Scale(coordinateScale);
			}

			return returnArray;
		}
		
		/// <summary>
		/// Converts and copies based on the specified coordinate scale. Also verify the tangent.w component to be -1 or 1
		/// </summary>
		/// <param name="array">The array to convert and copy</param>
		/// <param name="coordinateSpaceCoordinateScale">The specified coordinate space</param>
		/// <returns>The copied and converted coordinate space</returns>
		public static Vector4[] ConvertTangentCoordinateSpaceAndCopy(Vector4[] array, GLTF.Math.Vector4 coordinateSpaceCoordinateScale)
		{
			var returnArray = new Vector4[array.Length];
			var coordinateScale = coordinateSpaceCoordinateScale.ToUnityVector4Raw();

			for (var i = 0; i < array.Length; i++)
			{
				returnArray[i] = array[i];
				returnArray[i].w = Mathf.Sign(returnArray[i].w);
				returnArray[i].Scale(coordinateScale);
			}

			return returnArray;
		}		

		/// <summary>
		/// Rewinds the indicies into Unity coordinate space from glTF space
		/// </summary>
		/// <param name="attributeAccessor">The attribute accessor to modify</param>
		public static void FlipTriangleFaces(int[] indices)
		{
			for (int i = 0; i < indices.Length; i += 3)
			{
				int temp = indices[i];
				indices[i] = indices[i + 2];
				indices[i + 2] = temp;
			}
		}

		public static Matrix4x4 ToUnityMatrix4x4(this GLTF.Math.Matrix4x4 matrix)
		{
			return new Matrix4x4()
			{
				m00 = matrix.M11,
				m01 = matrix.M12,
				m02 = matrix.M13,
				m03 = matrix.M14,
				m10 = matrix.M21,
				m11 = matrix.M22,
				m12 = matrix.M23,
				m13 = matrix.M24,
				m20 = matrix.M31,
				m21 = matrix.M32,
				m22 = matrix.M33,
				m23 = matrix.M34,
				m30 = matrix.M41,
				m31 = matrix.M42,
				m32 = matrix.M43,
				m33 = matrix.M44
			};
		}

		public static Matrix4x4[] ToUnityMatrix4x4(this GLTF.Math.Matrix4x4[] inMatrixArr)
		{
			Matrix4x4[] outMatrixArr = new Matrix4x4[inMatrixArr.Length];
			for (int i = 0; i < inMatrixArr.Length; ++i)
			{
				outMatrixArr[i] = inMatrixArr[i].ToUnityMatrix4x4();
			}
			return outMatrixArr;
		}

		public static Quaternion SwitchHandedness(this Quaternion input)
		{
			return new Quaternion(-input.x, input.y, input.z, -input.w);
		}

		/*
		public static Vector4 SwitchHandedness(this Vector4 input)
		{
			return new Vector4(-input.x, input.y, input.z, -input.w);
		}

		public static Matrix4x4 SwitchHandedness(this Matrix4x4 matrix)
		{
			Vector3 position = matrix.GetColumn(3).SwitchHandedness();
			Quaternion rotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1)).SwitchHandedness();
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
		*/
	}
}
