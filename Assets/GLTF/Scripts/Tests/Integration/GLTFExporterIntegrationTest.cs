using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GLTF;

public class GLTFExporterIntegrationTest : MonoBehaviour {

	void Start () {
		var exporter = new GLTFExporter(new [] {transform});
		var root = exporter.GetRoot();

		var scene = root.GetDefaultScene();

		IntegrationTest.Assert(scene.Name == gameObject.name);



		IntegrationTest.Assert(root.Materials[0].AlphaMode == AlphaMode.BLEND);

		IntegrationTest.Pass();
	}

}
