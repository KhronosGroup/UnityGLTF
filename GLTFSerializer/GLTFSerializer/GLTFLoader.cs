using System;
using System.Threading.Tasks;
using System.Net;
#if WINDOWS_UWP
using Windows.Storage.Streams;
#endif

namespace GLTFSerializer
{
    public class GLTFLoader : IGLTFLoader
    {
        public enum MaterialType
        {
            PbrMetallicRoughness,
            PbrSpecularGlossiness,
            CommonConstant,
            CommonPhong,
            CommonBlinn,
            CommonLambert
        }

        private bool _multithreaded = true;
        public bool Multithreaded
        {
            get { return _multithreaded; }
            set { _multithreaded = value; }
        }

        // todo blgross- clean up this class to not store data in member variables
        private GLTFRoot _root;
        
        public async Task<GLTFRoot> Load(string gltfUrl)
        {
            // todo change to file load
            var www = WebRequest.Create(gltfUrl);
            WebResponse response = await www.GetResponseAsync();
            System.IO.Stream gltfData = response.GetResponseStream();
            return await Load(gltfData);
        }

        public Task<GLTFRoot> Load(System.IO.Stream stream)
        {
            // todo: this code does not work if file is greater than 4 gb
            int streamLength = (int)stream.Length;
            byte[] gltfData = new byte[streamLength];
            stream.Read(gltfData, 0, streamLength);
            if (Multithreaded)
            {
                return Task.Run(() =>
                {
                    return ParseGLTF(gltfData);    
                });
            }
            else
            {
                return Task.FromResult(ParseGLTF(gltfData));
            }
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

            if (Multithreaded)
            {
                return await Task.Run(() =>
                {
                    return ParseGLTF(gltfData);
                });
            }
            else
            {
                return ParseGLTF(gltfData);
            }
        }
#endif

        public Task<GLTFRoot> Load(byte[] gltfData)
        {
            if (Multithreaded)
            {
                return Task.Run(() =>
                {
                    return ParseGLTF(gltfData);
                });
            }
            else
            {
                return Task.FromResult(ParseGLTF(gltfData));
            }
        }

        private GLTFRoot ParseGLTF(byte[] gltfData)
        {
            byte[] glbBuffer;
            _root = GLTFParser.ParseBinary(gltfData, out glbBuffer);

            if (glbBuffer != null)
            {
                _root.Buffers[0].Contents = glbBuffer;
            }

            return _root;
        }
    }
}
