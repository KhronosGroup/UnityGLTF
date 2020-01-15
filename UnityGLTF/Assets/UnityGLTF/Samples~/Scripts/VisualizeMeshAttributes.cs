using UnityEngine;

namespace UnityGLTF.Examples
{
	public class VisualizeMeshAttributes : MonoBehaviour
	{
		[SerializeField] private MeshFilter Mesh = default;
		[SerializeField] private float NormalScale = 0.1f;
		[SerializeField] private float TangentScale = 0.1f;
		[SerializeField] private bool VisualizeTangents = false;
		[SerializeField] private bool VisualizeNormals = false;

		private Vector3[] vertices;
		private Vector3[] normals;
		private Vector4[] tangents;

		void OnEnable()
		{
			if (Mesh != null && Mesh.mesh != null)
			{
				vertices = Mesh.mesh.vertices;
				normals = Mesh.mesh.normals;
				tangents = Mesh.mesh.tangents;
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (vertices != null)
			{
				int numVerts = vertices.Length;
				for (int vertexIndex = 0; vertexIndex < numVerts; vertexIndex++)
				{
					var vertexTransformed = this.transform.TransformPoint(vertices[vertexIndex]);

					if (VisualizeNormals && normals != null)
					{
						var normalTransformed = this.transform.InverseTransformVector(normals[vertexIndex]);
						Debug.DrawLine(vertexTransformed, vertexTransformed + normalTransformed * NormalScale * 0.5f,
							Color.green);
						Debug.DrawLine(vertexTransformed + normalTransformed * NormalScale * 0.5f,
							vertexTransformed + normalTransformed * NormalScale * 1.0f, Color.blue);
					}

					if (VisualizeTangents && tangents != null)
					{
						var tangentTransformed = this.transform.TransformVector(
							tangents[vertexIndex].w * new Vector3(tangents[vertexIndex].x, tangents[vertexIndex].y,
								tangents[vertexIndex].z));
						Debug.DrawLine(vertexTransformed, vertexTransformed + tangentTransformed * TangentScale * 0.5f,
							Color.black);
						Debug.DrawLine(vertexTransformed + tangentTransformed * TangentScale * 0.5f,
							vertexTransformed + tangentTransformed * TangentScale * 1.0f, Color.white);
					}
				}
			}
		}
	}
}
