using System.Collections.Generic;
using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Extensions;
using LightType = UnityEngine.LightType;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private Dictionary<int, int> _exportedLights;

		private LightId ExportLight(Light unityLight)
        {
            GLTFLight light = null;

            if (unityLight.type == LightType.Spot)
            {
	            // TODO URP/HDRP can distinguish here, no need to guess innerConeAngle there
                light = new GLTFSpotLight() { innerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad * 0.8f, outerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad };
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = unityLight.color.ToNumericsColorLinear();
                light.range = unityLight.range;
                light.intensity = GraphicsSettings.lightsUseLinearIntensity ? 
                    (unityLight.intensity * Mathf.PI) :
                    // heuristically derived. Not sure why this matches expected light values / what Unity does when lightsUseLinearIntensity is false
                    (unityLight.intensity * Mathf.PI) * (unityLight.intensity * Mathf.PI) / 4 * Mathf.Sqrt(2);
            }
            else if (unityLight.type == LightType.Directional)
            {
                light = new GLTFDirectionalLight();
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = unityLight.color.ToNumericsColorLinear();
                light.intensity = unityLight.intensity * Mathf.PI;
                if (!GraphicsSettings.lightsUseLinearIntensity)
                    light.intensity *= unityLight.intensity;
                
            }
            else if (unityLight.type == LightType.Point)
            {
                light = new GLTFPointLight();
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = unityLight.color.ToNumericsColorLinear();
                light.range = unityLight.range;
                light.intensity = unityLight.intensity * Mathf.PI;
                if (!GraphicsSettings.lightsUseLinearIntensity)
                    light.intensity *= unityLight.intensity;
            }
            else
            {
	            // unknown light type, we shouldn't export this

	            // light = new GLTFLight();
                // light.Name = unityLight.name;
                // light.type = unityLight.type.ToString().ToLower();
                // light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
            }

            if (light != null)
            {
	            DeclareExtensionUsage(KHR_lights_punctualExtensionFactory.EXTENSION_NAME, false);
            }
            else
            {
	            Debug.LogWarning(null, $"Light couldn't be exported: {unityLight.name}. The type may be unsupported in glTF ({unityLight.type})", unityLight.gameObject);
	            return null;
            }

            if (_root.Lights == null)
            {
                _root.Lights = new List<GLTFLight>();
            }

            var id = new LightId
            {
                Id = _root.Lights.Count,
                Root = _root
            };

            // Register nodes for animation parsing (could be disabled if animation is disabled)
            _exportedLights.Add(unityLight.GetInstanceID(), _root.Lights.Count);

            //list of lightids should be in extensions object
            _root.Lights.Add(light);

            return id;
        }
	}
}
