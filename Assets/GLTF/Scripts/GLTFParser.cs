using System;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace GLTF
{

    public class GLTFParser
    {

        public static GLTFRoot Parse(string gltfUrl, string gltf)
        {
            return Parse(gltfUrl, new StringReader(gltf));
        }

        public static GLTFRoot Parse(string gltfUrl, TextReader gltfReader)
        {
            return GLTFRoot.Deserialize(gltfUrl, new JsonTextReader(gltfReader));
        }

	    public static GLTFRoot Parse(string gltfUrl, byte[] gltfBinary)
	    {
		    string gltfContent;
		    byte[] gltfBinaryChunk = null;

		    // Check for binary format magic bytes
		    if (BitConverter.ToUInt32(gltfBinary, 0) == 0x46546c67)
		    {
			    // Parse header information

			    var version = BitConverter.ToUInt32(gltfBinary, 4);
			    if (version != 2)
			    {
				    throw new GLTFHeaderInvalidException("Unsupported glTF version");
			    }

			    var length = BitConverter.ToUInt32(gltfBinary, 8);
			    if (length != gltfBinary.Length)
			    {
				    throw new GLTFHeaderInvalidException("File length does not match header.");
			    }

			    var chunkLength = BitConverter.ToUInt32(gltfBinary, 12);
			    var chunkType = BitConverter.ToUInt32(gltfBinary, 16);
			    if (chunkType != (uint)ChunkFormat.JSON)
			    {
				    throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
			    }

			    // Load JSON chunk
			    gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)chunkLength);

			    // Load Binary Chunk
			    if (20 + chunkLength < length)
			    {
				    var start = 20 + (int)chunkLength;
				    chunkLength = BitConverter.ToUInt32(gltfBinary, start);
				    if (start + chunkLength > length)
				    {
					    throw new GLTFHeaderInvalidException("File length does not match chunk header.");
				    }

				    chunkType = BitConverter.ToUInt32(gltfBinary, start + 4);
				    if (chunkType != (uint)ChunkFormat.BIN)
				    {
					    throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
				    }

				    gltfBinaryChunk = new byte[chunkLength];
				    Buffer.BlockCopy(gltfBinary, start + 8, gltfBinaryChunk, 0, (int)chunkLength);
			    }
		    }
		    else
		    {
			    gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary);
		    }

		    var stringReader = new StringReader(gltfContent);
			var root = GLTFRoot.Deserialize(gltfUrl, new JsonTextReader(stringReader));

			if (gltfBinaryChunk != null)
		    {
			    if (root.Buffers == null || root.Buffers.Count == 0)
			    {
				    throw new Exception("Binary buffer not defined in buffers array.");
			    }

			    // Replace GLTFBuffer so we reference the binary chunk's data
			    root.Buffers[0] = new GLTFGLBBuffer(root.Buffers[0], gltfBinaryChunk);
		    }

		    return root;
	    }
	}

    public enum ChunkFormat : uint
    {
        JSON = 0x4e4f534a,
        BIN = 0x004e4942
    }

}