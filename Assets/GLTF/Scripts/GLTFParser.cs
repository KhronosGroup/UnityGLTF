using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace GLTF
{

    public class GLTFParser
    {

        public GLTFRoot Parse(string gltfPath, string gltf)
        {
            return Parse(gltfPath, new StringReader(gltf));
        }

        public GLTFRoot Parse(string gltfPath, TextReader gltfReader)
        {
            return GLTFRoot.Deserialize(gltfPath, new JsonTextReader(gltfReader));
        }
    }

    public enum ChunkFormat : uint
    {
        JSON = 0x4e4f534a,
        BIN = 0x004e4942
    }

}