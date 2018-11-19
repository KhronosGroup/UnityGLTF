using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace GLTF.Schema
{
	/// <summary>
	/// glTF extension that defines the LOD 
	/// </summary>
	public class MSFT_LODExtension : IExtension
	{

		public List<int> MeshIds { get; set; }
		public MSFT_LODExtension(List<int> meshIds)
		{
			MeshIds = meshIds;
		}
		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new MSFT_LODExtension(MeshIds);
		}
		public JProperty Serialize()
		{
			JProperty jProperty = new JProperty(MSFT_LODExtensionFactory.EXTENSION_NAME,
				new JObject(
					new JProperty(MSFT_LODExtensionFactory.IDS, new JArray(MeshIds))
					)
				);
			return jProperty;
		}
	}
}
