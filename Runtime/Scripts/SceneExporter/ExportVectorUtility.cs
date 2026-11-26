using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF
{
	/// <summary>
	/// Converts mesh normals and tangents to glTF space and makes them valid for glTF, in one Burst compiled pass.
	/// glTF 2.0 requires NORMAL and the xyz of TANGENT to be normalized, TANGENT.w to be 1 or -1, and floats to be
	/// neither NaN nor infinite (3.6.2.2, 3.7.2.1). Unity meshes don't guarantee that: RecalculateNormals leaves the
	/// normals of vertices that are only used by line or point submeshes at zero, merged submeshes without tangents
	/// get zero tangents, decoders and scripts can write anything. The spec gives no tolerance, so vectors that are off
	/// by more than float rounding are normalized; morph target displacements have no such requirement.
	/// </summary>
	internal static class ExportVectorUtility
	{
		private const float UnitLengthSqrTolerance = 1e-5f;
		private const float ZeroLengthSqr = 1e-12f;

		/// <summary>
		/// Returns the normals scaled into glTF space (like SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy) and
		/// normalized; zero-length, NaN and infinite normals become (0, 1, 0).
		/// </summary>
		/// <param name="replaced">The number of replaced normals.</param>
		public static unsafe Vector3[] ConvertNormals(Vector3[] normals, Vector3 coordinateScale, out int replaced)
		{
			var result = new Vector3[normals.Length];
			replaced = 0;
			if (normals.Length == 0) return result;

			var source = UnsafeUtility.PinGCArrayAndGetDataAddress(normals, out var sourceHandle);
			var target = UnsafeUtility.PinGCArrayAndGetDataAddress(result, out var targetHandle);
			try
			{
				int count;
				new ConvertNormalsJob
				{
					Source = (float3*)source,
					Result = (float3*)target,
					Length = normals.Length,
					Scale = coordinateScale,
					Replaced = &count,
				}.Run();
				replaced = count;
			}
			finally
			{
				UnsafeUtility.ReleaseGCObject(sourceHandle);
				UnsafeUtility.ReleaseGCObject(targetHandle);
			}
			return result;
		}

		/// <summary>
		/// Returns the tangents scaled into glTF space (like SchemaExtensions.ConvertTangentCoordinateSpaceAndCopy, w
		/// becomes ±1) with a normalized xyz; zero-length, NaN and infinite ones become perpendicular to the normal.
		/// </summary>
		/// <param name="gltfNormals">The normals returned by <see cref="ConvertNormals"/>, or null.</param>
		/// <param name="replaced">The number of replaced tangents.</param>
		public static unsafe Vector4[] ConvertTangents(Vector4[] tangents, Vector4 coordinateScale, Vector3[] gltfNormals, out int replaced)
		{
			var result = new Vector4[tangents.Length];
			replaced = 0;
			if (tangents.Length == 0) return result;

			var hasNormals = gltfNormals != null && gltfNormals.Length == tangents.Length;
			var source = UnsafeUtility.PinGCArrayAndGetDataAddress(tangents, out var sourceHandle);
			var target = UnsafeUtility.PinGCArrayAndGetDataAddress(result, out var targetHandle);
			ulong normalsHandle = 0;
			var normals = hasNormals ? UnsafeUtility.PinGCArrayAndGetDataAddress(gltfNormals, out normalsHandle) : null;
			try
			{
				int count;
				new ConvertTangentsJob
				{
					Source = (float4*)source,
					Result = (float4*)target,
					Normals = (float3*)normals,
					Length = tangents.Length,
					Scale = coordinateScale,
					Replaced = &count,
				}.Run();
				replaced = count;
			}
			finally
			{
				UnsafeUtility.ReleaseGCObject(sourceHandle);
				UnsafeUtility.ReleaseGCObject(targetHandle);
				if (hasNormals) UnsafeUtility.ReleaseGCObject(normalsHandle);
			}
			return result;
		}

		/// <summary>True when all vertices referenced by the indices have zero-length, NaN or infinite normals.</summary>
		public static unsafe bool HasOnlyZeroNormals(Vector3[] normals, int[] indices)
		{
			if (indices.Length == 0) return false;

			var normalsPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(normals, out var normalsHandle);
			var indicesPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(indices, out var indicesHandle);
			try
			{
				bool result;
				new HasOnlyZeroNormalsJob
				{
					Normals = (float3*)normalsPtr,
					NormalsLength = normals.Length,
					Indices = (int*)indicesPtr,
					IndicesLength = indices.Length,
					Result = &result,
				}.Run();
				return result;
			}
			finally
			{
				UnsafeUtility.ReleaseGCObject(normalsHandle);
				UnsafeUtility.ReleaseGCObject(indicesHandle);
			}
		}

		// NaN fails every comparison, so these are written to be true for NaN: !(a > b) instead of a <= b
		private static bool IsZeroLength(float sqrMagnitude) => !(sqrMagnitude > ZeroLengthSqr) || math.isinf(sqrMagnitude);
		private static bool IsUnitLength(float sqrMagnitude) => math.abs(sqrMagnitude - 1) <= UnitLengthSqrTolerance;

		[BurstCompile(CompileSynchronously = true)]
		private unsafe struct ConvertNormalsJob : IJob
		{
			[NativeDisableUnsafePtrRestriction] public float3* Source;
			[NativeDisableUnsafePtrRestriction] public float3* Result;
			public int Length;
			public float3 Scale;
			[NativeDisableUnsafePtrRestriction] public int* Replaced;

			public void Execute()
			{
				var replaced = 0;
				for (var i = 0; i < Length; i++)
				{
					var n = Source[i] * Scale;
					var sqr = math.lengthsq(n);
					if (!IsUnitLength(sqr))
					{
						if (IsZeroLength(sqr))
						{
							n = new float3(0, 1, 0);
							replaced++;
						}
						else n /= math.sqrt(sqr);
					}
					Result[i] = n;
				}
				*Replaced = replaced;
			}
		}

		[BurstCompile(CompileSynchronously = true)]
		private unsafe struct ConvertTangentsJob : IJob
		{
			[NativeDisableUnsafePtrRestriction] public float4* Source;
			[NativeDisableUnsafePtrRestriction] public float4* Result;
			/// <summary>glTF space normals, null without normals.</summary>
			[NativeDisableUnsafePtrRestriction] public float3* Normals;
			public int Length;
			public float4 Scale;
			[NativeDisableUnsafePtrRestriction] public int* Replaced;

			public void Execute()
			{
				var replaced = 0;
				for (var i = 0; i < Length; i++)
				{
					var t = Source[i];
					t.w = t.w >= 0 ? 1 : -1; // Mathf.Sign, also -1 for NaN
					t *= Scale;
					var xyz = t.xyz;
					var sqr = math.lengthsq(xyz);
					if (!IsUnitLength(sqr))
					{
						if (IsZeroLength(sqr))
						{
							if (Normals != null)
							{
								var n = Normals[i];
								xyz = math.normalize(math.cross(math.abs(n.y) < 0.99f ? new float3(0, 1, 0) : new float3(1, 0, 0), n));
							}
							else xyz = new float3(1, 0, 0);
							replaced++;
						}
						else xyz /= math.sqrt(sqr);
					}
					Result[i] = new float4(xyz, t.w);
				}
				*Replaced = replaced;
			}
		}

		[BurstCompile(CompileSynchronously = true)]
		private unsafe struct HasOnlyZeroNormalsJob : IJob
		{
			[NativeDisableUnsafePtrRestriction] public float3* Normals;
			public int NormalsLength;
			[NativeDisableUnsafePtrRestriction] public int* Indices;
			public int IndicesLength;
			[NativeDisableUnsafePtrRestriction] public bool* Result;

			public void Execute()
			{
				for (var i = 0; i < IndicesLength; i++)
				{
					var index = Indices[i];
					if (index < 0 || index >= NormalsLength || !IsZeroLength(math.lengthsq(Normals[index])))
					{
						*Result = false;
						return;
					}
				}
				*Result = true;
			}
		}
	}
}
