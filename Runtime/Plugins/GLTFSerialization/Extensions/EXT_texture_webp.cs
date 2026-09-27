using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	[Serializable]
	public class EXT_texture_webp : IExtension
	{
		public const string EXTENSION_NAME = "EXT_texture_webp";
		public ImageId source;

		public EXT_texture_webp(ImageId source)
		{
			this.source = source;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new EXT_texture_webp(source);
		}

		public JProperty Serialize()
		{
			JObject ext = new JObject();

			ext.Add("source", source.Id);

			return new JProperty(EXTENSION_NAME, ext);
		}
	}

	public class EXT_texture_webp_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = EXT_texture_webp.EXTENSION_NAME;

		public EXT_texture_webp_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				if (extensionToken.Value["source"] == null)
					throw new Exception("EXT_texture_webp extension must contain a source property.");

				var reader = extensionToken.Value["source"].CreateReader();
				var extension = new EXT_texture_webp(ImageId.Deserialize(root, reader));
				return extension;
			}

			return null;
		}
	}
}
