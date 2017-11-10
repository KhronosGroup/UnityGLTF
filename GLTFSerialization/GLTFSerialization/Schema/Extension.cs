sing Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public interface IExtension
	{
	    IExtension Clone();
		JProperty Serialize();
	}

	public abstract class ExtensionFactory
	{
		public string ExtensionName;
		public abstract IExtension Deserialize(GLTFRoot root, JProperty extensionToken);
	}

	public class DefaultExtension : IExtension
	{
		public JProperty ExtensionData { get; internal set; }

	    public IExtension Clone()
	    {
	        return new DefaultExtension
	        {
                ExtensionData = new JProperty(ExtensionData)
	        };
	    }

	    public JProperty Serialize()
		{
			return ExtensionData;
		}
	}

	public class DefaultExtensionFactory : ExtensionFactory
	{
		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			return new DefaultExtension
			{
				ExtensionData = extensionToken
			};
		}
	}
}