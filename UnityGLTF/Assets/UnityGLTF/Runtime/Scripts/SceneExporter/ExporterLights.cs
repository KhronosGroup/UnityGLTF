using System.Collections.Generic;
using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using UnityEngine;
using LightType = UnityEngine.LightType;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private Dictionary<int, int> _exportedLights;

		private LightId ExportLight(Light unityLight)
        {
	        DeclareExtensionUsage(KHR_lights_punctualExtensionFactory.EXTENSION_NAME, false);

            GLTFLight light;

            if (unityLight.type == LightType.Spot)
            {
	            // TODO URP/HDRP can distinguish here, no need to guess innerConeAngle there
                light = new GLTFSpotLight() { innerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad * 0.8f, outerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad };
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.range = unityLight.range;
                light.intensity = unityLight.intensity * Mathf.PI;
            }
            else if (unityLight.type == LightType.Directional)
            {
                light = new GLTFDirectionalLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.intensity = unityLight.intensity * Mathf.PI;
            }
            else if (unityLight.type == LightType.Point)
            {
                light = new GLTFPointLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.range = unityLight.range;
                light.intensity = unityLight.intensity * Mathf.PI;
            }
            else
            {
                light = new GLTFLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
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
