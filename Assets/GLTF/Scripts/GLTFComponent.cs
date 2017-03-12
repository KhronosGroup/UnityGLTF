using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GLTF {

    class GLTFComponent : MonoBehaviour
    {

        public string url = "http://localhost:8080/samples/Lantern/glTF/Lantern.gltf";
        private GameObject scene;

        IEnumerator Start()
        {
            UnityWebRequest www = UnityWebRequest.Get(url);

            yield return www.Send();

            if (www.isError)
            {
                if (www.responseCode == 404)
                {
                    throw new Exception("GLTF file could not be found. Are you sure the url is correct?");
                }
                else if (www.responseCode == 0)
                {
                    throw new Exception("Could not connect to the host. Have you started the web server yet?");
                }
                else
                {
                    throw new Exception(www.error);
                }
            }

            GLTFRoot gltf = GLTFParser.Parse(url, www.downloadHandler.data);

            if (gltf.scenes == null || gltf.scenes.Length == 0)
            {
                throw new Exception("No scene in gltf file.");
            }

            yield return gltf.LoadAllScenes();

            if (gltf.scene != null)
            {
                scene = gltf.scene.Value.Create(gameObject);
            }
            else
            {
                scene = gltf.scenes[0].Create(gameObject);
            }
        }
    }
}
