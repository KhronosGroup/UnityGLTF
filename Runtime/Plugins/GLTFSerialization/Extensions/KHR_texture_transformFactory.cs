using System;
using Newtonsoft.Json.Linq;
using GLTF.Extensions;
using GLTF.Math;

namespace GLTF.Schema
{
	public class ExtTextureTransformExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_texture_transform";
		public const string OFFSET = "offset";
		public const string ROTATION = "rotation";
		public const string SCALE = "scale";
		public const string TEXCOORD = "texCoord";

		public ExtTextureTransformExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			Vector2 offset = new Vector2(ExtTextureTransformExtension.OFFSET_DEFAULT);
			double rotation = 0;
			Vector2 scale = new Vector2(ExtTextureTransformExtension.SCALE_DEFAULT);
			Nullable<int> texCoord = null;

			if (extensionToken != null)
			{
				JToken offsetToken = extensionToken.Value[OFFSET];
				offset = offsetToken != null ? offsetToken.DeserializeAsVector2() : offset;

				JToken rotationToken = extensionToken.Value[ROTATION];
				rotation = rotationToken != null ? rotationToken.DeserializeAsDouble() : rotation;

				JToken scaleToken = extensionToken.Value[SCALE];
				scale = scaleToken != null ? scaleToken.DeserializeAsVector2() : scale;

				JToken texCoordToken = extensionToken.Value[TEXCOORD];
				texCoord = texCoordToken != null ? texCoordToken.DeserializeAsInt() : texCoord;
			}
			
			return new ExtTextureTransformExtension(offset, rotation, scale, texCoord);
		}
	}
}
