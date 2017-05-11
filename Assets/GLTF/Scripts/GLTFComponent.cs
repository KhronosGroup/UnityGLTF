using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;

namespace GLTF {

    class GLTFComponent : MonoBehaviour
    {
        public string Url;
	    public bool Multithreaded = true;

        public int MaximumLod = 300;

        public Shader GLTFStandard;

        IEnumerator Start()
        {
            var loader = new GLTFLoader(
                Url,
                GLTFStandard,
                gameObject.transform
            );
	        loader.Multithreaded = Multithreaded;
            loader.MaximumLod = MaximumLod;
            yield return loader.Load();
        }
    }
}
