using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
	internal static class LightmapUVGeneration
	{
		/// <summary>
		/// Generates lightmap UVs while keeping blend shapes intact.
		/// <see cref="Unwrapping.GenerateSecondaryUVSet(Mesh, UnwrapParam)"/> reorders vertices and splits them along
		/// UV seams. It moves all vertex streams (positions, normals, UVs, bone weights, ...) along, but it leaves the
		/// blend shape deltas untouched, so afterwards they sit on the wrong vertices and the shapes are broken.
		/// To work around that we tag every vertex with its original index in an unused UV channel. The unwrapper
		/// carries that tag along like any other vertex data, so it tells us where each resulting vertex came from
		/// and we can rebuild the blend shapes from the original deltas.
		/// </summary>
		internal static void GenerateSecondaryUVSet(Mesh mesh, UnwrapParam unwrapSettings)
		{
			// Channel 0 holds the base UVs and channel 1 is where the unwrapper writes the lightmap UVs,
			// so the tag has to live in one of the remaining channels.
			var markerChannel = -1;
			for (var c = 7; c >= 2; c--)
			{
				if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0 + c)) continue;
				markerChannel = c;
				break;
			}

			if (mesh.blendShapeCount == 0 || markerChannel < 0)
			{
				if (mesh.blendShapeCount > 0)
					Debug.LogWarning($"Can't preserve the blend shapes of mesh \"{mesh.name}\" while generating lightmap UVs because all UV channels are in use. The blend shapes will be broken.");
				Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);
				return;
			}

			var originalVertexCount = mesh.vertexCount;
			var shapes = ReadBlendShapes(mesh);

			var marker = new Vector2[originalVertexCount];
			for (var i = 0; i < originalVertexCount; i++)
				marker[i] = new Vector2(i, 0);
			mesh.SetUVs(markerChannel, marker);

			// Remove the shapes before unwrapping: the vertex count can change, which would leave them dangling.
			mesh.ClearBlendShapes();

			Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);

			var newVertexCount = mesh.vertexCount;
			var markerAfterUnwrap = new List<Vector2>(newVertexCount);
			mesh.GetUVs(markerChannel, markerAfterUnwrap);
			// Drop the tag again, it's not part of the imported mesh.
			mesh.SetUVs(markerChannel, (Vector2[])null);

			if (markerAfterUnwrap.Count != newVertexCount)
			{
				Debug.LogWarning($"Lost the vertex mapping while generating lightmap UVs for mesh \"{mesh.name}\", its blend shapes will be missing.");
				return;
			}

			var originalIndices = new int[newVertexCount];
			var invalidIndices = 0;
			for (var i = 0; i < newVertexCount; i++)
			{
				var originalIndex = Mathf.RoundToInt(markerAfterUnwrap[i].x);
				if (originalIndex < 0 || originalIndex >= originalVertexCount)
				{
					originalIndex = 0;
					invalidIndices++;
				}
				originalIndices[i] = originalIndex;
			}

			if (invalidIndices > 0)
				Debug.LogWarning($"{invalidIndices} vertices of mesh \"{mesh.name}\" couldn't be mapped back to the original mesh while generating lightmap UVs, its blend shapes may be broken.");

			WriteBlendShapes(mesh, shapes, originalIndices);
		}

		private readonly struct BlendShapeFrameData
		{
			public readonly string Name;
			public readonly float Weight;
			public readonly Vector3[] DeltaVertices;
			public readonly Vector3[] DeltaNormals;
			public readonly Vector3[] DeltaTangents;

			public BlendShapeFrameData(string name, float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents)
			{
				Name = name;
				Weight = weight;
				DeltaVertices = deltaVertices;
				DeltaNormals = deltaNormals;
				DeltaTangents = deltaTangents;
			}
		}

		private static List<BlendShapeFrameData> ReadBlendShapes(Mesh mesh)
		{
			var vertexCount = mesh.vertexCount;
			var frames = new List<BlendShapeFrameData>(mesh.blendShapeCount);

			for (var shapeIndex = 0; shapeIndex < mesh.blendShapeCount; shapeIndex++)
			{
				var name = mesh.GetBlendShapeName(shapeIndex);
				var frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
				for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
				{
					var deltaVertices = new Vector3[vertexCount];
					var deltaNormals = new Vector3[vertexCount];
					var deltaTangents = new Vector3[vertexCount];
					mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

					// Unity fills these with zeroes when the shape doesn't have normals/tangents.
					// Passing null instead keeps the rebuilt shape identical to the original one.
					frames.Add(new BlendShapeFrameData(
						name,
						mesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex),
						deltaVertices,
						HasAnyDelta(deltaNormals) ? deltaNormals : null,
						HasAnyDelta(deltaTangents) ? deltaTangents : null));
				}
			}

			return frames;
		}

		private static void WriteBlendShapes(Mesh mesh, List<BlendShapeFrameData> frames, int[] originalIndices)
		{
			foreach (var frame in frames)
			{
				mesh.AddBlendShapeFrame(frame.Name, frame.Weight,
					Remap(frame.DeltaVertices, originalIndices),
					Remap(frame.DeltaNormals, originalIndices),
					Remap(frame.DeltaTangents, originalIndices));
			}
		}

		private static Vector3[] Remap(Vector3[] deltas, int[] originalIndices)
		{
			if (deltas == null) return null;
			var result = new Vector3[originalIndices.Length];
			for (var i = 0; i < originalIndices.Length; i++)
				result[i] = deltas[originalIndices[i]];
			return result;
		}

		private static bool HasAnyDelta(Vector3[] deltas)
		{
			foreach (var delta in deltas)
				if (delta != Vector3.zero)
					return true;
			return false;
		}
	}
}
