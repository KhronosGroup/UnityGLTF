using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace GLTF
{

    public class GLTFParser
    {

        public GLTFRoot Parse(string gltfUrl, string gltf)
        {
            return Parse(gltfUrl, new StringReader(gltf));
        }

        public GLTFRoot Parse(string gltfUrl, TextReader gltfReader)
        {
            return GLTFRoot.Deserialize(gltfUrl, new JsonTextReader(gltfReader));
        }
    }

    public enum ChunkFormat : uint
    {
        JSON = 0x4e4f534a,
        BIN = 0x004e4942
    }

}