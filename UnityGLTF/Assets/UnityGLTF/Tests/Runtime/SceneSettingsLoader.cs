using UnityEngine;

namespace UnityGLTFTests
{
	[RequireComponent(typeof(Skybox))]
	public class SceneSettingsLoader : MonoBehaviour
	{
		public void Awake()
		{
			// Apply the provided skybox to the entire scene
			RenderSettings.skybox = GetComponent<Skybox>().material;
			DynamicGI.UpdateEnvironment();
		}
	}
}
