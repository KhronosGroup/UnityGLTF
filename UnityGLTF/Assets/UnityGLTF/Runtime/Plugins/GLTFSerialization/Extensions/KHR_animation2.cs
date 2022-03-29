using GLTF.Schema;
using Newtonsoft.Json.Linq;

namespace UnityGLTF.Extensions
{

	public class KHR_animation2 : IExtension
	{
		public const string EXTENSION_NAME = "KHR_animation2";

		public object animatedObject;
		public string propertyBinding;
		public string path;
		public AnimationChannelTarget channel;

		public JProperty Serialize()
		{
			return new JProperty(EXTENSION_NAME, new JObject(new JProperty("path", path)));
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_animation2()
			{
				animatedObject = animatedObject,
				propertyBinding = propertyBinding,
				path = path,
				channel = channel
			};
		}
	}
}
