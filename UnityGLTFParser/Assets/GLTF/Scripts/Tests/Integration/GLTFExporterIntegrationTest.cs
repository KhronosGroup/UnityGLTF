using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTFSerialization;

public class GLTFExporterIntegrationTest : MonoBehaviour {

#if false
    void Start () {
        var exporter = new GLTFExporter(new [] {transform});
        var root = exporter.GetRoot();

        var scene = root.GetDefaultScene();

        IntegrationTest.Assert(scene.Name == gameObject.name);



        IntegrationTest.Assert(root.Materials[0].AlphaMode == GLTFJsonSerialization.AlphaMode.BLEND);

        IntegrationTest.Pass();
    }
#endif

}
