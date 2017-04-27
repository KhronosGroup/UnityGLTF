using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;

namespace GLTF {

    class GLTFComponent : MonoBehaviour
    {
        public string url = "http://localhost:8080/samples/Lantern/glTF/Lantern.gltf";

        IEnumerator Start()
        {
            var loader = new GLTFLoader(url, gameObject.transform);
            yield return loader.Load();
        }
    }
}
