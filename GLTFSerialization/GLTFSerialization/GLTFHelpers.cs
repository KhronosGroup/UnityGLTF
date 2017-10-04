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
				var attributeAccessor = attributes[SemanticProperties.POSITION];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsVertexArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.INDICES))
			{
				var attributeAccessor = attributes[SemanticProperties.INDICES];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsTriangles(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.NORMAL))
			{
				var attributeAccessor = attributes[SemanticProperties.NORMAL];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsNormalArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(0)))
			{
				var attributeAccessor = attributes[SemanticProperties.TexCoord(0)];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsTexcoordArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(1)))
			{
				var attributeAccessor = attributes[SemanticProperties.TexCoord(1)];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsTexcoordArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(2)))
			{
				var attributeAccessor = attributes[SemanticProperties.TexCoord(2)];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsTexcoordArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TexCoord(3)))
			{
				var attributeAccessor = attributes[SemanticProperties.TexCoord(3)];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsTexcoordArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.Color(0)))
			{
				var attributeAccessor = attributes[SemanticProperties.Color(0)];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsColorArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
			if (attributes.ContainsKey(SemanticProperties.TANGENT))
			{
				var attributeAccessor = attributes[SemanticProperties.TANGENT];
				NumericArray resultArray = attributeAccessor.AccessorContent;
				int offset = (int)LoadBufferView(attributeAccessor, out byte[] bufferViewCache);
				attributeAccessor.AccessorId.Value.AsTangentArray(ref resultArray, bufferViewCache, offset);
				attributeAccessor.AccessorContent = resultArray;
			}
		}

		private static long LoadBufferView(AttributeAccessor attributeAccessor, out byte[] bufferViewCache)
		{
			BufferView bufferView = attributeAccessor.AccessorId.Value.BufferView.Value;
			long totalOffset = bufferView.ByteOffset + attributeAccessor.Offset;
#if !NETFX_CORE
			if (attributeAccessor.Stream is System.IO.MemoryStream)
			{
				using (var memoryStream = attributeAccessor.Stream as System.IO.MemoryStream)
				{
					bufferViewCache = memoryStream.GetBuffer();
					return totalOffset;
				}
			}
#endif
			attributeAccessor.Stream.Position = totalOffset;
			bufferViewCache = new byte[bufferView.ByteLength];
			attributeAccessor.Stream.Read(bufferViewCache, 0, bufferView.ByteLength);
			return 0;
		}
	}
}
