using UnityEngine;
using System.Collections;
using GLTF;

public class GLTFTestComponent : MonoBehaviour {
    public string Url;
    public Shader Shader;
    public bool Multithreaded = true;

    IEnumerator Start()
    {
        var loader = new GLTFLoader(Url, Shader, gameObject.transform);
        loader.Multithreaded = Multithreaded;
        yield return loader.Load();
        IntegrationTest.Pass();
    }
}
