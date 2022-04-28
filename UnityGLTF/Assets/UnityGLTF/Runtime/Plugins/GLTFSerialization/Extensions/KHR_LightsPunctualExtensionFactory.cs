

using Newtonsoft.Json.Linq;

namespace GLTF.Schema.KHR_lights_punctual
{
	public class KHR_lights_punctualExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_lights_punctual";


		public const string PNAME_LIGHTS = "lights";
		public const string PNAME_LIGHT  = "light";

		public KHR_lights_punctualExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				JToken lightsToken = extensionToken.Value[PNAME_LIGHTS];

				if (lightsToken != null)
				{
					var extension = new KHR_LightsPunctualExtension();
					JArray lightsArray = lightsToken as JArray;
					foreach( var lightToken in lightsArray.Children())
					{
						extension.Lights.Add(PunctualLight.Deserialize(root, lightToken));
					}
					return extension;
				}

				JToken nodelightToken = extensionToken.Value[PNAME_LIGHT];

				if (nodelightToken != null)
				{
					PunctualLightId lightId = PunctualLightId.Deserialize(root, nodelightToken.CreateReader() );
					return new KHR_LightsPunctualNodeExtension( lightId, root );
				}
			}

			return null;
		}
	}
}
