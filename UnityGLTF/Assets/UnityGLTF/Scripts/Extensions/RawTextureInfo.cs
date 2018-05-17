using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// Represents the header information in a raw texture (DDS or CRN) that is needed to construct a Texture2D object with matching properties.
    /// </summary>
    public class RawTextureInfo
    {
        public int Width;
        public int Height;
        public bool HasMips;
        public TextureFormat Format;
        public byte[] RawData;
    }
}