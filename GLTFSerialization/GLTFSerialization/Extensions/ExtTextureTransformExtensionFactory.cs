using Newtonsoft.Json.Linq;
using GLTF.Extensions;
using GLTF.Math;

namespace GLTF.Schema
{
	public class ExtTextureTransformExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "EXT_texture_transform";
		public const string OFFSET = "offset";
		public const string SCALE = "scale";
		public const string TEXCOORD = "texCoord";

		public ExtTextureTransformExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			Vector2 offset = new Vector2(ExtTextureTransformExtension.OFFSET_DEFAULT);
			Vector2 scale = new Vector2(ExtTextureTransformExtension.SCALE_DEFAULT);
			int texCoord = ExtTextureTransformExtension.TEXCOORD_DEFAULT;

			if (extensionToken != null)
			{
				JToken offsetToken = extensionToken.Value[OFFSET];
				offset = offsetToken != null ? offsetToken.DeserializeAsVector2() : offset;

				JToken scaleToken = extensionToken.Value[SCALE];
				scale = scaleToken != null ? scaleToken.DeserializeAsVector2() : scale;

				JToken texCoordToken = extensionToken.Value[TEXCOORD];
				texCoord = texCoordToken != null ? texCoordToken.DeserializeAsInt() : texCoord;
			}
			
			return new ExtTextureTransformExtension(offset, scale, texCoord);
		}
	}
}
