using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	/// <summary>
	/// General interface for extensions
	/// </summary>
	public interface IExtension
	{
		/// <summary>
		/// Serializes the extension to a property
		/// </summary>
		/// <returns>JProperty of extension</returns>
		JProperty Serialize();

		/// <summary>
		/// Clones the extension. When implementing note that root can be null if the extension is not attached to a GLTFChildOfRootProperty
		/// </summary>
		/// <param name="root">GLTFRoot is availible</param>
		/// <returns>Cloned version of the extension</returns>
		IExtension Clone(GLTFRoot root);
	}

	/// <summary>
	/// Abstract class for factory which creates an extension.
	/// </summary>
	public abstract class ExtensionFactory
	{
		/// <summary>
		/// Name of the extension being created
		/// </summary>
		public string ExtensionName;

		/// <summary>
		/// Deserializes the input token
		/// </summary>
		/// <param name="root">Root node if needed for deserailization</param>
		/// <param name="extensionToken">The token data</param>
		/// <returns></returns>
		public abstract IExtension Deserialize(GLTFRoot root, JProperty extensionToken);
	}

	/// <summary>
	/// Default implementation of extension in order to preserve any non explicitly overriden extension in the JSON
	/// </summary>
	public class DefaultExtension : IExtension
	{
		/// <summary>
		/// Extenion data as a JProperty
		/// </summary>
		public JProperty ExtensionData { get; internal set; }

		public IExtension Clone(GLTFRoot root)
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

	/// <summary>
	/// Default implementation of ExtensionFactory to keep around any extensions not directly referenced
	/// </summary>
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