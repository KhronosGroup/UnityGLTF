using GLTF.Schema;
using Newtonsoft.Json.Linq;

namespace UnityGLTF.Extensions
{

	// see https://github.com/KhronosGroup/glTF/pull/2147
	public class KHR_animation_pointer : IExtension
	{
		public const string EXTENSION_NAME = "KHR_animation_pointer";

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
			return new KHR_animation_pointer()
			{
				animatedObject = animatedObject,
				propertyBinding = propertyBinding,
				path = path,
				channel = channel
			};
		}
	}
}
