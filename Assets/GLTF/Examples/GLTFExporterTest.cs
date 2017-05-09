using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GLTF;

public class GLTFExporterTest : MonoBehaviour {

	// Use this for initialization
	void Awake () {
		var exporter = new GLTFExporter(transform);
		var output = exporter.SerializeGLTF();
		Debug.Log(output);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
