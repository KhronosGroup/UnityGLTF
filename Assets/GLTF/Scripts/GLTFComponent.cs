using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;

namespace GLTF {

	class GLTFComponent : MonoBehaviour
	{
		public string Url;
		public bool Multithreaded = true;

		public int MaximumLod = 300;

		public Shader GLTFStandard;

		IEnumerator Start()
		{
			var loader = new GLTFLoader(
				Url,
				gameObject.transform
			);
			loader.SetShaderForMaterialType(GLTFLoader.MaterialType.PbrMetallicRoughness, GLTFStandard);
			loader.Multithreaded = Multithreaded;
			loader.MaximumLod = MaximumLod;
			yield return loader.Load();
		}
	}
}
