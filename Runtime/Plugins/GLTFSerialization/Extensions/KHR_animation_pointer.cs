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

		// when cloned, the path is late resolved in gltf exporter so the clone path will not be resolved because it wont be registered to the exporter's resolver
		private KHR_animation_pointer clonedFrom;

		public JProperty Serialize()
		{
			if (path == null && clonedFrom != null) path = clonedFrom.path;
			return new JProperty(EXTENSION_NAME, new JObject(new JProperty("pointer", path)));
		}
		
		public void Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			var extensionObject = (JObject) extensionToken.Value;
			path = (string) extensionObject["pointer"];
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_animation_pointer
			{
				animatedObject = animatedObject,
				propertyBinding = propertyBinding,
				path = path,
				channel = channel,
				clonedFrom = this
			};
		}
	}
}
