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
        public Shader GLTFStandardAlphaBlend;
        public Shader GLTFStandardAlphaMask;

        IEnumerator Start()
        {
            var loader = new GLTFLoader(Url, gameObject.transform);
	        loader.Multithreaded = Multithreaded;
            loader.MaximumLod = MaximumLod;
            loader.GLTFStandard = GLTFStandard;
            loader.GLTFStandardAlphaBlend = GLTFStandardAlphaBlend;
            loader.GLTFStandardAlphaMask = GLTFStandardAlphaMask;
            yield return loader.Load();
        }
    }
}
