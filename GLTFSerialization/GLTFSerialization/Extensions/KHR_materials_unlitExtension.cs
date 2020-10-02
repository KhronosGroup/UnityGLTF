using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	/// <summary>
	/// glTF extension that defines the unlit material model 
	/// 
	/// Spec can be found here:
	/// https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_unlit
	/// </summary>
	public class KHR_materials_unlitExtension : IExtension
	{
		public KHR_materials_unlitExtension() { }

		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new KHR_materials_unlitExtension();
		}

		public JProperty Serialize()
		{
			JProperty jProperty =
				new JProperty(KHR_materials_unlitExtensionFactory.EXTENSION_NAME,
					new JObject()
					);

			return jProperty;
		}
	}
}
