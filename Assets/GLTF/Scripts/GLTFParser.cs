using Newtonsoft.Json;
using System;

namespace GLTF
{
    public class GLTFParser
    {
        /// <summary>
        /// Parse the gltf data as a JSON string into a GLTFRoot object.
        /// </summary>
        /// <param name="gltfPath">The path/url to the GLTF file.</param>
        /// <param name="gltf">The gltf data as a JSON string.</param>
        /// <returns>The parsed GLTFRoot object.</returns>
        public static GLTFRoot Parse(string gltfPath, byte[] gltfBinary)
        {
            GLTFRootRef rootRef = new GLTFRootRef();
            string gltfContent;
            byte[] gltfBinaryChunk = null;

            // Check for binary format magic bytes
            if(BitConverter.ToUInt32(gltfBinary, 0) == 0x46546c67)
            {
                // Parse header information

                uint version = BitConverter.ToUInt32(gltfBinary, 4);
                if(version != 2)
                {
                    throw new GLTFHeaderInvalidException("Unsupported glTF version");
                }

                uint length = BitConverter.ToUInt32(gltfBinary, 8);
                if(length != gltfBinary.Length)
                {
                    throw new GLTFHeaderInvalidException("File length does not match header.");
                }

                uint chunkLength = BitConverter.ToUInt32(gltfBinary, 12);
                uint chunkType = BitConverter.ToUInt32(gltfBinary, 16);
                if(chunkType != (uint)ChunkFormat.JSON)
                {
                    throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
                }

                // Load JSON chunk
                gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)chunkLength);

                // Load Binary Chunk
                if (20 + chunkLength < length)
                {
                    int start = 20 + (int)chunkLength;
                    chunkLength = BitConverter.ToUInt32(gltfBinary, start);
                    if(start + chunkLength > length)
                    {
                        throw new GLTFHeaderInvalidException("File length does not match chunk header.");
                    }

                    chunkType = BitConverter.ToUInt32(gltfBinary, start + 4);
                    if(chunkType != (uint)ChunkFormat.BIN)
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

            // Register all of the JSON converters for the various ID types.
            GLTFRoot root = JsonConvert.DeserializeObject<GLTFRoot>(
                gltfContent,
                new GLTFUriConverter(gltfPath),
                new GLTFAccessorIdConverter(rootRef),
                new GLTFBufferIdConverter(rootRef),
                new GLTFBufferViewIdConverter(rootRef),
                new GLTFCameraIdConverter(rootRef),
                new GLTFImageIdConverter(rootRef),
                new GLTFMaterialIdConverter(rootRef),
                new GLTFMeshIdConverter(rootRef),
                new GLTFNodeIdConverter(rootRef),
                new GLTFSamplerIdConverter(rootRef),
                new GLTFSceneIdConverter(rootRef),
                new GLTFSkinIdConverter(rootRef),
                new GLTFTextureIdConverter(rootRef)
            );

            if (gltfBinaryChunk != null)
            {
                if (root.buffers == null || root.buffers.Length == 0)
                {
                    throw new Exception("Binary buffer not defined in buffers array.");
                }

                // Replace GLTFBuffer so we reference the binary chunk's data
                root.buffers[0] = new GLTFInternalBuffer(root.buffers[0], gltfBinaryChunk);
            }

            rootRef.root = root;

            return root;
        }
    }

    public enum ChunkFormat : uint
    {
        JSON = 0x4e4f534a,
        BIN = 0x004e4942
    }
}