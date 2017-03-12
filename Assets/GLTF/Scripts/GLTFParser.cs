using Newtonsoft.Json;
using System;

namespace GLTF {
    public class GLTFParser
    {
        public enum ContentFormat : uint
        {
            JSON = 0x0
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
            byte[] unifiedBuffer;

            // check for binary format magic bytes
            byte[] b = gltfBinary;
            if (b[0] == 0x67 && b[1] == 0x6c && b[2] == 0x54 && b[3] == 0x46)
            {
                // parse header information

                //uint version = BitConverter.ToUInt32(gltfBinary, 4);

                uint length = BitConverter.ToUInt32(gltfBinary, 8);
                if( length != gltfBinary.Length)
                {
                    throw new GLTF.GLTFHeaderInvalidException("File length does not match header.");
                }

                uint contentLength = BitConverter.ToUInt32(gltfBinary, 12);
                uint contentFormat = BitConverter.ToUInt32(gltfBinary, 16);
                // test models not compliant
                /*if(contentFormat != (uint)ContentFormat.JSON)
                {
                    throw new GLTF.GLTFHeaderInvalidException("Content format not recognized");
                }*/

                // load content
                gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)contentLength);

                // load integrated buffer
                int unifiedLength = (int)(length - contentLength - 20);
                unifiedBuffer = new byte[unifiedLength];
                Array.ConstrainedCopy(gltfBinary, (int)contentLength + 20, unifiedBuffer, 0, unifiedLength);
                }
                else
                {
                gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary);
                unifiedBuffer = new byte[0];
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