using Newtonsoft.Json;
using System;

namespace GLTF {
    public class GLTFParser
    {
        public enum ChunkFormat : uint
        {
            JSON = 0x4e4f534a,
            BIN = 0x004e4942
        }

        /// <summary>
        /// Parse the gltf data as a JSON string into a GLTFRoot object.
        /// </summary>
        /// <param name="gltfPath">The path/url to the GLTF file.</param>
        /// <param name="gltf">The gltf data as a JSON string.</param>
        /// <returns>The parsed GLTFRoot object.</returns>
        public static GLTFRoot Parse (string gltfPath, byte[] gltfBinary)
        {
            GLTFRootRef rootRef = new GLTFRootRef();
            string gltfContent;

            // check for binary format magic bytes
            if( BitConverter.ToUInt32(gltfBinary, 0) == 0x46546c67 )
            {
                // parse header information

                uint version = BitConverter.ToUInt32(gltfBinary, 4);
                if( version != 2)
                {
                    throw new GLTFHeaderInvalidException("Unsupported glTF version");
                }

                uint length = BitConverter.ToUInt32(gltfBinary, 8);
                if( length != gltfBinary.Length)
                {
                    throw new GLTFHeaderInvalidException("File length does not match header.");
                }

                uint chunkLength = BitConverter.ToUInt32(gltfBinary, 12);
                uint chunkFormat = BitConverter.ToUInt32(gltfBinary, 16);
                if(chunkFormat != (uint)ChunkFormat.JSON)
                {
                    throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
                }

                // load content
                gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)chunkLength);

                // load integrated buffer
                if (20 + chunkLength < length)
                {
                    int start = 20 + (int)chunkLength;
                    chunkLength = BitConverter.ToUInt32(gltfBinary, start);
                    if(start + chunkLength > length)
                    {
                        throw new GLTFHeaderInvalidException("File length does not match chunk header.");
                    }

                    chunkFormat = BitConverter.ToUInt32(gltfBinary, start + 4);
                    if(chunkFormat != (uint)ChunkFormat.BIN)
                    {
                        throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
                    }

                    rootRef.internalDataBuffer = new byte[chunkLength];
                    Array.ConstrainedCopy(gltfBinary, start + 8, rootRef.internalDataBuffer, 0, (int)chunkLength);
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

            rootRef.root = root;

            return root;
        }
    }
}