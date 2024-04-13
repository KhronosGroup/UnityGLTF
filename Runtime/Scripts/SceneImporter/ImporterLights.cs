using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using UnityEngine;
using UnityGLTF.Extensions;
using UnityGLTF.Plugins;
using LightType = UnityEngine.LightType;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		private bool ConstructLights(GameObject nodeObj, Node node)
		{
			var useLightsExtension = Context.TryGetPlugin<LightsPunctualImportContext>(out _);
			if (!useLightsExtension) return false;
			
			// TODO this should be handled by the lights extension directly, not here
			const string lightExt = KHR_lights_punctualExtensionFactory.EXTENSION_NAME;
			KHR_LightsPunctualNodeExtension lightsExtension = null;
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(lightExt) && node.Extensions != null && node.Extensions.ContainsKey(lightExt))
			{
				lightsExtension = node.Extensions[lightExt] as KHR_LightsPunctualNodeExtension;
				var l = lightsExtension.LightId;

				var light = l.Value;

				var newLight = nodeObj.AddComponent<Light>();
				switch (light.Type)
				{
					case GLTF.Schema.KHR_lights_punctual.LightType.spot:
						newLight.intensity = (float) light.Intensity / Mathf.PI;
						newLight.type = LightType.Spot;
						break;
					case GLTF.Schema.KHR_lights_punctual.LightType.directional:
						newLight.type = LightType.Directional;
						break;
					case GLTF.Schema.KHR_lights_punctual.LightType.point:
						newLight.type = LightType.Point;
						newLight.intensity = (float) light.Intensity * Mathf.PI;
						break;
				}

				newLight.name = light.Name;
				newLight.color = light.Color.ToUnityColorRaw();
				newLight.range = (float) light.Range;
				if (light.Spot != null)
				{
#if UNITY_2019_1_OR_NEWER
					newLight.innerSpotAngle = (float) light.Spot.InnerConeAngle * 2 / (Mathf.Deg2Rad * 0.8f);
#endif
					newLight.spotAngle = (float) light.Spot.OuterConeAngle * 2 / Mathf.Deg2Rad;
				}

				nodeObj.transform.localRotation *= SchemaExtensions.InvertDirection;
				return true;
			}

			return false;
		}
	}
}
