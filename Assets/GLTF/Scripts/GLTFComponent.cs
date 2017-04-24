using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;

namespace GLTF {

    class GLTFComponent : MonoBehaviour
    {
        public string url = "http://localhost:8080/samples/Lantern/glTF/Lantern.gltf";
        private GameObject scene;
        public GLTFRoot gltf;
        private bool workerThreadRunning = false;

        IEnumerator Start()
        {

            var www = UnityWebRequest.Get(url);

            yield return www.Send();

            var gltfData = www.downloadHandler.data;

            yield return ParseGLTF(gltfData);

	        var scene = gltf.GetDefaultScene();

			if (scene == null)
            {
                throw new Exception("No default scene in gltf file.");
            }

            foreach (var buffer in gltf.Buffers)
            {
                yield return buffer.Load();
            }

            foreach (var image in gltf.Images)
            {
                yield return image.Load();
            }

            yield return BuildVertexAttributes();

			scene.Create(gameObject);
		}

        private IEnumerator ParseGLTF(byte[] gltfData)
        {
            workerThreadRunning = true;

            ThreadPool.QueueUserWorkItem((_) =>
            {
                gltf = GLTFParser.Parse(url, gltfData);

                workerThreadRunning = false;
            });

            yield return Wait();
        }

        private IEnumerator BuildVertexAttributes()
        {
            workerThreadRunning = true;

            ThreadPool.QueueUserWorkItem((_) =>
            {

                foreach (var mesh in gltf.Meshes)
                {
                    mesh.BuildVertexAttributes();
                }

                workerThreadRunning = false;
            });

            yield return Wait();
        }

        private IEnumerator Wait()
        {
            while (workerThreadRunning)
            {
                yield return null;
            }
        }
    }
}
