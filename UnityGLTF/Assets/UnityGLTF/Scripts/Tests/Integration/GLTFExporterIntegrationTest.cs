using UnityEngine;

namespace UnityGLTF.Tests.Integration
{
#if UNITY_5
    public class GLTFExporterIntegrationTest : MonoBehaviour
	{
		public string RetrieveTexturePath(UnityEngine.Texture texture)
		{
			return texture.name;
		}

		void Start()
		{
			var exporter = new GLTFSceneExporter(new[] {transform}, RetrieveTexturePath);
			exporter.SaveGLTFandBin("tempDir", "test");
			var root = exporter.GetRoot();

			var scene = root.GetDefaultScene();

			IntegrationTest.Assert(scene.Name == gameObject.name);


			IntegrationTest.Assert(root.Materials[0].AlphaMode == GLTF.Schema.AlphaMode.BLEND);

			IntegrationTest.Pass();
		}
	}
#endif
}
