using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage.Streams;
#endif

namespace GLTFJsonSerialization
{
    public class GLTFJsonLoader : IGLTFJsonLoader
    {
        // todo blgross- clean up this class to not store data in member variables
        private GLTFRoot _root;

        public GLTFRoot Load(System.IO.Stream stream)
        {
            // todo: this code does not work if file is greater than 4 gb
            int streamLength = (int)stream.Length;
            byte[] gltfData = new byte[streamLength];
            stream.Read(gltfData, 0, streamLength);

            return ParseGLTF(gltfData);
        }

#if WINDOWS_UWP
        public async Task<GLTFRoot> Load(IRandomAccessStream stream)
        {
            // todo: this code does not work if file is greater than 4 gb
            uint streamLength = (uint)stream.Size;
            DataReader reader = new DataReader(stream.GetInputStreamAt(0));
            byte[] gltfData = new byte[streamLength];
            await reader.LoadAsync(streamLength);
            reader.ReadBytes(gltfData);
            return ParseGLTF(gltfData);
        }
#endif

        public GLTFRoot Load(byte[] gltfData)
        {
            return ParseGLTF(gltfData);
        }

        private GLTFRoot ParseGLTF(byte[] gltfData)
        {
            byte[] glbBuffer;
            _root = GLTFParser.Parse(gltfData, out glbBuffer);

            if (glbBuffer != null)
            {
                // todo blgross fix GLB parsing
                //_root.Buffers[0].Contents = glbBuffer;
            }

            return _root;
        }
    }
}
