using GLTF.Schema;
using Newtonsoft.Json.Linq;

namespace UnityGLTF.Extensions
{
	public class KHR_animation2 : IExtension
	{
		public const string EXTENSION_NAME = "KHR_animation2";

		private readonly string path;

		public KHR_animation2(string path)
		{
			this.path = path;
		}

		public JProperty Serialize()
		{
			return new JProperty(EXTENSION_NAME, new JObject(new JProperty("path", path)));
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_animation2(path);
		}
	}
}
