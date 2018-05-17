using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// See the following reference for reading CRN files
    /// https://github.com/BinomialLLC/crunch/blob/master/inc/crn_decomp.h
    /// </summary>
    public class CRNHeader
    {
        abstract class crn_packed_uint
        {
            byte[] m_buf;
            
            public crn_packed_uint(int size, BinaryReader br)
            {
                m_buf = new byte[size];
                m_buf = br.ReadBytes(m_buf.Length);
            }
            
            public uint Value {
                get
                {
                    switch (m_buf.Length)
                    {
                        case 1: return m_buf[0];
                        case 2: return ((uint)m_buf[0] << 8) | (uint)m_buf[1];
                        case 3: return ((uint)m_buf[0] << 16) | ((uint)m_buf[1] << 8) | ((uint)m_buf[2]);
                        default: return ((uint)m_buf[0] << 24) | ((uint)m_buf[1] << 16) | ((uint)m_buf[2] << 8) | ((uint)m_buf[3]);
                    }
                }
            }            
        };

        class crn_packed_uint1 : crn_packed_uint
        {
            public crn_packed_uint1(BinaryReader br) : base(1, br) { }
        }

        class crn_packed_uint2 : crn_packed_uint
        {
            public crn_packed_uint2(BinaryReader br) : base(2, br) { }
        }

        class crn_packed_uint3 : crn_packed_uint
        {
            public crn_packed_uint3(BinaryReader br) : base(3, br) { }
        }

        class crn_packed_uint4 : crn_packed_uint
        {
            public crn_packed_uint4(BinaryReader br) : base(4, br) { }
        }

        private struct CRNHeaderStruct
        {
            public const int CRN_SIG_VALUE = ('H' << 8) | 'x';

            public crn_packed_uint2 m_sig;
            public crn_packed_uint2 m_header_size;
            public crn_packed_uint2 m_header_crc16;

            public crn_packed_uint4 m_data_size;
            public crn_packed_uint2 m_data_crc16;

            public crn_packed_uint2 m_width;
            public crn_packed_uint2 m_height;

            public crn_packed_uint1 m_levels;
            public crn_packed_uint1 m_faces;

            public crn_packed_uint1 m_format;
            public crn_packed_uint2 m_flags;

            public crn_packed_uint4 m_reserved;
            public crn_packed_uint4 m_userdata0;
            public crn_packed_uint4 m_userdata1;

            public struct crn_palette
            {
                public crn_packed_uint3 m_ofs;
                public crn_packed_uint3 m_size;
                public crn_packed_uint2 m_num;

                public crn_palette(BinaryReader br)
                {
                    m_ofs = new crn_packed_uint3(br);
                    m_size = new crn_packed_uint3(br);
                    m_num = new crn_packed_uint2(br);
                }
            }

            public crn_palette m_color_endpoints;
            public crn_palette m_color_selectors;

            public crn_palette m_alpha_endpoints;
            public crn_palette m_alpha_selectors;

            public crn_packed_uint2 m_tables_size;
            public crn_packed_uint3 m_tables_ofs;

            // m_level_ofs[] is actually an array of offsets: m_level_ofs[m_levels]
            public crn_packed_uint4 m_level_ofs;

            public static CRNHeaderStruct Read(BinaryReader br)
            {
                CRNHeaderStruct header = new CRNHeaderStruct();

                header.m_sig = new crn_packed_uint2(br);
                header.m_header_size = new crn_packed_uint2(br);
                header.m_header_crc16 = new crn_packed_uint2(br);

                header.m_data_size = new crn_packed_uint4(br);
                header.m_data_crc16 = new crn_packed_uint2(br);

                header.m_width = new crn_packed_uint2(br);
                header.m_height = new crn_packed_uint2(br);

                header.m_levels = new crn_packed_uint1(br);
                header.m_faces = new crn_packed_uint1(br);

                header.m_format = new crn_packed_uint1(br);
                header.m_flags = new crn_packed_uint2(br);

                header.m_reserved = new crn_packed_uint4(br);
                header.m_userdata0 = new crn_packed_uint4(br);
                header.m_userdata1 = new crn_packed_uint4(br);

                header.m_color_endpoints = new crn_palette(br);
                header.m_color_selectors = new crn_palette(br);

                header.m_alpha_endpoints = new crn_palette(br);
                header.m_alpha_selectors = new crn_palette(br);

                header.m_tables_size = new crn_packed_uint2(br);
                header.m_tables_ofs = new crn_packed_uint3(br);

                header.m_level_ofs = new crn_packed_uint4(br);

                return header;
            }
        }

        const int cCRNFmtDXT1 = 0;
        const int cCRNFmtDXT5 = 2;

        public static RawTextureInfo Read(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    CRNHeaderStruct header = CRNHeaderStruct.Read(br);
                    uint sig = header.m_sig.Value;
                    if(sig != CRNHeaderStruct.CRN_SIG_VALUE)
                    {
                        return null;
                    }
                    var info = new RawTextureInfo();
                    info.Width = (int)header.m_width.Value;
                    info.Height = (int) header.m_height.Value;
                    info.HasMips = header.m_levels.Value > 1;
                    uint format = header.m_format.Value;
                    if (format == cCRNFmtDXT1)
                    {
                        info.Format = TextureFormat.DXT1Crunched;
                    }
                    else if (format == cCRNFmtDXT5)
                    {
                        info.Format = TextureFormat.DXT5Crunched;
                    }
                    else
                    {
                        Debug.LogError("Unknown crn format = " + header.m_format.Value);
                    }
                    info.RawData = data;
                    return info;
                }
            }
        }
    }
}