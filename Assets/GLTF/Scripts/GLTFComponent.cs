using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;

namespace GLTF {

    class GLTFComponent : MonoBehaviour
    {
        public string Url;
	    public Shader Shader;
	    public bool Multithreaded = true;

        IEnumerator Start()
        {
            var loader = new GLTFLoader(Url, Shader, gameObject.transform);
	        loader.Multithreaded = Multithreaded;
            yield return loader.Load();
        }
    }
}
