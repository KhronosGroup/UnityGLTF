using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class EXT_texture_exr : IExtension
	{
		public const string EXTENSION_NAME = "EXT_texture_exr";
		public ImageId source;

		public EXT_texture_exr(ImageId source)
		{
			this.source = source;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new EXT_texture_exr(source);
		}

		public JProperty Serialize()
		{
			JObject ext = new JObject();

			ext.Add("source", source.Id);

			return new JProperty(EXTENSION_NAME, ext);
		}
	}
}
