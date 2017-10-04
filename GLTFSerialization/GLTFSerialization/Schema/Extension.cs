using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public interface Extension
	{
		JProperty Serialize();
	}

	public abstract class ExtensionFactory
	{
		public string ExtensionName;
		public abstract Extension Deserialize(GLTFRoot root, JProperty extensionToken);
	}

	public class DefaultExtension : Extension
	{
		public JProperty ExtensionData { get; internal set; }

		public JProperty Serialize()
		{
			return ExtensionData;
		}
	}

	public class DefaultExtensionFactory : ExtensionFactory
	{
		public override Extension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			return new DefaultExtension
			{
				ExtensionData = extensionToken
			};
		}
	}
}
