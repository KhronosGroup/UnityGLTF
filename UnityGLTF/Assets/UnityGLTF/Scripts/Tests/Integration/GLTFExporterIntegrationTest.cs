using UnityEngine;

namespace UnityGLTF.Tests.Integration
{
	public class GLTFExporterIntegrationTest : MonoBehaviour
	{
		void Start()
		{
			var exporter = new GLTFSceneExporter(new[] {transform});
			exporter.SaveGLTFandBin("tempDir", "test");
			var root = exporter.GetRoot();

			var scene = root.GetDefaultScene();

			IntegrationTest.Assert(scene.Name == gameObject.name);


			IntegrationTest.Assert(root.Materials[0].AlphaMode == GLTF.Schema.AlphaMode.BLEND);

			IntegrationTest.Pass();
		}
	}
}
