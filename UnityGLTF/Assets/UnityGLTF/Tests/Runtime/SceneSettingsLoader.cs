using UnityEngine;

namespace UnityGLTFTests
{
	[RequireComponent(typeof(Skybox))]
	public class SceneSettingsLoader : MonoBehaviour
	{
		[SerializeField]
		private float ambientIntensity = 1;

		public void Awake()
		{
			// Apply provided settings to the entire scene
			RenderSettings.ambientIntensity = ambientIntensity;
			RenderSettings.skybox = GetComponent<Skybox>().material;

			DynamicGI.UpdateEnvironment();
		}
	}
}
