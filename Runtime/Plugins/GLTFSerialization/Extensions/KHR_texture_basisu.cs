using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	[Serializable]
	public class KHR_texture_basisu : IExtension
	{
		public const string EXTENSION_NAME = "KHR_texture_basisu";
		public ImageId source;

		public KHR_texture_basisu(ImageId source)
		{
			this.source = source;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_texture_basisu(source);
		}

		public JProperty Serialize()
		{
			JObject ext = new JObject();

			ext.Add("source", source.Id);

			return new JProperty(EXTENSION_NAME, ext);
		}
	}

	public class KHR_texture_basisu_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = KHR_texture_basisu.EXTENSION_NAME;

		public KHR_texture_basisu_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				if (extensionToken.Value["source"] == null)
					throw new Exception("KHR_texture_basisu extension must contain a source property.");

				var reader = extensionToken.Value["source"].CreateReader();
				var extension = new KHR_texture_basisu(ImageId.Deserialize(root, reader));
				return extension;
			}

			return null;
		}
	}
}
