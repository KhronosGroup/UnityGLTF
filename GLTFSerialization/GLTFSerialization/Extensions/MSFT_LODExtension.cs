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
			throw new NullReferenceException();
		}
		public JProperty Serialize()
		{
			throw new NullReferenceException();
			//return new JProperty(ExtTextureTransformExtensionFactory.EXTENSION_NAME, ext);
		}
	}
}
