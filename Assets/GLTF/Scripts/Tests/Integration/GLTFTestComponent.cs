using UnityEngine;
using System.Collections;
using GLTF;

public class GLTFTestComponent : MonoBehaviour {
    public string Url;
    public bool Multithreaded = true;

    public Shader GLTFStandard;
    public Shader GLTFStandardAlphaBlend;
    public Shader GLTFStandardAlphaMask;


    IEnumerator Start()
    {
        var loader = new GLTFLoader(
            Url,
            GLTFStandard,
            GLTFStandardAlphaBlend,
            GLTFStandardAlphaMask,
            gameObject.transform
        );
        loader.Multithreaded = Multithreaded;
        yield return loader.Load();
        IntegrationTest.Pass();
    }
}
