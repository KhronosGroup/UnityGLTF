using Newtonsoft.Json.Linq;
using GLTF.Extensions;
using GLTF.Math;

namespace GLTF.Schema
{
	public class KHR_lights_punctualExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_lights_punctual";
		//public const string OFFSET = "offset";
		//public const string SCALE = "scale";
		//public const string TEXCOORD = "texCoord";

		public const string COLOR = "color";
		public const string TYPE = "type";

		public KHR_lights_punctualExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			Color color = new Color(KHR_lights_punctualExtension.COLOR_DEFAULT);
			string type = KHR_lights_punctualExtension.TYPE_DEFAULT;

			if (extensionToken != null)
			{
				JToken colorToken = extensionToken.Value[COLOR];
				color = colorToken != null ? colorToken.DeserializeAsColor() : color;

				JToken typeToken = extensionToken.Value[TYPE];
				type = typeToken != null ? typeToken.Value<string>() : type;
			}

			//Vector2 offset = new Vector2(KHR_lights_punctualExtension.OFFSET_DEFAULT);
			//Vector2 scale = new Vector2(KHR_lights_punctualExtension.SCALE_DEFAULT);
			//int texCoord = KHR_lights_punctualExtension.TEXCOORD_DEFAULT;
			//
			//if (extensionToken != null)
			//{
			//	JToken offsetToken = extensionToken.Value[OFFSET];
			//	offset = offsetToken != null ? offsetToken.DeserializeAsVector2() : offset;
			//
			//	JToken scaleToken = extensionToken.Value[SCALE];
			//	scale = scaleToken != null ? scaleToken.DeserializeAsVector2() : scale;
			//
			//	JToken texCoordToken = extensionToken.Value[TEXCOORD];
			//	texCoord = texCoordToken != null ? texCoordToken.DeserializeAsInt() : texCoord;
			//}
			
			return new KHR_lights_punctualExtension(null);
		}
	}
}
