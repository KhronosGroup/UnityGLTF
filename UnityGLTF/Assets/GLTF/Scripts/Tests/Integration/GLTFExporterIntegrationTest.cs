using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTFSerialization;

public class GLTFExporterIntegrationTest : MonoBehaviour {
	
	void Start () {
		var exporter = new GLTFSceneExporter(new [] {transform});
		exporter.SaveGLTFandBin("tempDir", "test");
		var root = exporter.GetRoot();

		var scene = root.GetDefaultScene();

		IntegrationTest.Assert(scene.Name == gameObject.name);



		IntegrationTest.Assert(root.Materials[0].AlphaMode == GLTFSerialization.AlphaMode.BLEND);

		IntegrationTest.Pass();
	}

}
