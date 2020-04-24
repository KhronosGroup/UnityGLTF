using UnityEngine;

namespace UnityGLTFTests
{
	[RequireComponent(typeof(Skybox))]
	public class SceneSettingsLoader : MonoBehaviour
	{
		public void Awake()
		{
			// Apply provided settings to the entire scene
			RenderSettings.skybox = GetComponent<Skybox>().material;
			DynamicGI.UpdateEnvironment();
		}
	}
}
