using System.Collections.Generic;
using GLTF.Schema;

namespace GLTF
{
	public static class GLTFHelpers
	{
		/// <summary>
		/// Uses the accessor to parse the buffer into attributes needed to construct the mesh primitive
		/// </summary>
		/// <param name="attributes">A dictionary that contains a mapping of attribute name to data needed to parse</param>
		public static void BuildMeshAttributes(ref Dictionary<string, AttributeAccessor> attributes)
		{
			if (attributes.ContainsKey(SemanticProperties.POSITION))
			{
				var attributeBuilder = attributes[SemanticProperties.POSITION];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsVertexArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.INDICES))
			{
				var attributeBuilder = attributes[SemanticProperties.INDICES];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsTriangles(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.NORMAL))
			{
				var attributeBuilder = attributes[SemanticProperties.NORMAL];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsNormalArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(0)))
			{
				var attributeBuilder = attributes[SemanticProperties.TexCoord(0)];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsTexcoordArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(1)))
			{
				var attributeBuilder = attributes[SemanticProperties.TexCoord(1)];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsTexcoordArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(2)))
			{
				var attributeBuilder = attributes[SemanticProperties.TexCoord(2)];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsTexcoordArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(3)))
			{
				var attributeBuilder = attributes[SemanticProperties.TexCoord(3)];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsTexcoordArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.Color(0)))
			{
				var attributeBuilder = attributes[SemanticProperties.Color(0)];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsColorArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TANGENT))
			{
				var attributeBuilder = attributes[SemanticProperties.TANGENT];
				NumericArray resultArray = attributeBuilder.AccessorContent;
				attributeBuilder.AccessorId.Value.AsTangentArray(ref resultArray, attributeBuilder.Buffer);
				attributeBuilder.AccessorContent = resultArray;
			}
		}

		public static Math.Vector4[] ParseRotationKeyframes(Accessor accessor, byte[] bufferData)
		{
			NumericArray array = new NumericArray();
			return accessor.AsVector4Array(ref array, bufferData, true);
		}

		public static Math.Vector3[] ParseVector3Keyframes(Accessor accessor, byte[] bufferData)
		{
			NumericArray array = new NumericArray();
			return accessor.AsVector3Array(ref array, bufferData, false);
		}

		public static float[] ParseKeyframeTimes(Accessor accessor, byte[] bufferData)
		{
			NumericArray array = new NumericArray();
			return accessor.AsFloatArray(ref array, bufferData);
		}

		public static float[] ParseMorphWeights(Accessor accessor, byte[] bufferData)
		{
			NumericArray array = new NumericArray();
			return accessor.AsFloatArray(ref array, bufferData);
		}
	}
}
