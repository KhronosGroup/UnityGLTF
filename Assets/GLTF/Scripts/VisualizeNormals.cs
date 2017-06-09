using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizeNormals : MonoBehaviour
{
	[SerializeField] private MeshFilter Mesh;
	[SerializeField] private float NormalScale = 0.1f;

	private Vector3[] vertices;
	private Vector3[] normals;

	void OnEnable ()
	{
		if (Mesh != null && Mesh.mesh != null)
		{
			vertices = Mesh.mesh.vertices;
			normals = Mesh.mesh.normals;
		}
	}

	// Update is called once per frame
	void Update ()
	{
		if (vertices != null)
		{
			int numVerts = vertices.Length;
			for (int vertexIndex = 0; vertexIndex < numVerts; vertexIndex++)
			{
				var vertexTransformed = this.transform.TransformPoint(vertices[vertexIndex]);
				var normalTransformed = this.transform.InverseTransformVector(normals[vertexIndex]);
				Debug.DrawLine(vertexTransformed, vertexTransformed + normalTransformed*NormalScale,
					Color.blue);
			}
		}
	}
}
