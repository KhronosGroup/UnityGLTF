using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// This code has been ported from the following C# reference implementation for reading DDS files
    /// https://gist.github.com/soeminnminn/e9c4c99867743a717f5b
    /// </summary>
    public class DDSHeader
    {
        private struct DDSHeaderStruct
        {
            public uint size;		// equals size of struct (which is part of the data file!)
            public uint flags;
            public uint height;
            public uint width;
            public uint sizeorpitch;
            public uint depth;
            public uint mipmapcount;
            public uint alphabitdepth;
            //[MarshalAs(UnmanagedType.U4, SizeConst = 11)]
            public uint[] reserved;//[11];

            public struct pixelformatstruct
            {
                public uint size;	// equals size of struct (which is part of the data file!)
                public uint flags;
                public uint fourcc;
                public uint rgbbitcount;
                public uint rbitmask;
                public uint gbitmask;
                public uint bbitmask;
                public uint alphabitmask;
            }
            public pixelformatstruct pixelformat;

            public struct ddscapsstruct
            {
                public uint caps1;
                public uint caps2;
                public uint caps3;
                public uint caps4;
            }
            public ddscapsstruct ddscaps;
            public uint texturestage;
        }


        private static bool ReadHeader(BinaryReader reader, ref DDSHeaderStruct header)
        {
            byte[] signature = reader.ReadBytes(4);
            if (!(signature[0] == 'D' && signature[1] == 'D' && signature[2] == 'S' && signature[3] == ' '))
                return false;

            header.size = reader.ReadUInt32();
            if (header.size != 124)
                return false;

            //convert the data
            header.flags = reader.ReadUInt32();
            header.height = reader.ReadUInt32();
            header.width = reader.ReadUInt32();
            header.sizeorpitch = reader.ReadUInt32();
            header.depth = reader.ReadUInt32();
            header.mipmapcount = reader.ReadUInt32();
            header.alphabitdepth = reader.ReadUInt32();

            header.reserved = new uint[10];
            for (int i = 0; i < 10; i++)
            {
                header.reserved[i] = reader.ReadUInt32();
            }

            //pixelfromat
            header.pixelformat.size = reader.ReadUInt32();
            header.pixelformat.flags = reader.ReadUInt32();
            header.pixelformat.fourcc = reader.ReadUInt32();
            header.pixelformat.rgbbitcount = reader.ReadUInt32();
            header.pixelformat.rbitmask = reader.ReadUInt32();
            header.pixelformat.gbitmask = reader.ReadUInt32();
            header.pixelformat.bbitmask = reader.ReadUInt32();
            header.pixelformat.alphabitmask = reader.ReadUInt32();

            //caps
            header.ddscaps.caps1 = reader.ReadUInt32();
            header.ddscaps.caps2 = reader.ReadUInt32();
            header.ddscaps.caps3 = reader.ReadUInt32();
            header.ddscaps.caps4 = reader.ReadUInt32();
            header.texturestage = reader.ReadUInt32();

            return true;
        }

        const uint FOURCC_NO_COMPRESSION = 0;
        const uint FOURCC_DXT1 = 0x31545844;
        const uint FOURCC_DXT5 = 0x35545844;

        public static RawTextureInfo Read(byte[] data)
        {
            using(var ms  = new MemoryStream(data))
            {
                using(var br  = new BinaryReader(ms))
                {
                    DDSHeaderStruct header = new DDSHeaderStruct();
                    if(ReadHeader(br, ref header))
                    {
                        RawTextureInfo info = new RawTextureInfo();
                        info.Width = (int) header.width;
                        info.Height = (int) header.height;
                        info.HasMips = header.mipmapcount > 1;
                        switch(header.pixelformat.fourcc)
                        {

                            case FOURCC_NO_COMPRESSION:
                                if (header.pixelformat.rgbbitcount == 8)
                                {
                                    info.Format = TextureFormat.Alpha8;
                                }
                                else if (header.pixelformat.rgbbitcount == 32)
                                {
                                    info.Format = TextureFormat.RGBA32;
                                } else
                                {
                                    Debug.LogError("Invalid texture format fourcc = " + header.pixelformat.fourcc + " rgbbitcount = " + header.pixelformat.rgbbitcount);
                                }
                                break;
                            case FOURCC_DXT1:
                                info.Format = TextureFormat.DXT1;
                                break;
                            case FOURCC_DXT5:
                                info.Format = TextureFormat.DXT5;
                                break;
                            default:
                                Debug.LogError("Unrecognized texture format fourcc = " + header.pixelformat.fourcc);
                                break;
                        }
                        info.RawData = new byte[ms.Length - ms.Position];
                        ms.Read(info.RawData, 0, info.RawData.Length);
                        return info;
                    }                    
                }
            }
            return null;
        }
    }
}