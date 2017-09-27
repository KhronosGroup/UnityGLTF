using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;
using UnityEngine.Networking;

namespace GLTF {

	class GLTFComponent : MonoBehaviour
	{
		[Serializable]
		public struct Header {
			public string key;
			public string value;
		}

		public GameObject webServer;

		public string Url;
		public Header[] Headers;
		public bool Multithreaded = true;

		public int MaximumLod = 300;

		public Shader GLTFStandard;
		public Shader GLTFConstant;

		IEnumerator Start()
		{
			/* prevent race condition with web server */
			while (webServer == null) 
			{
				yield return null;
			}
			WebServerComponent component = webServer.GetComponent<WebServerComponent> ();
			while (!component.isRunning) 
			{
				yield return null;
			}

			/* loading GLTF */
			var loader = new GLTFLoader(
				Url,
				gameObject.transform, 
				Headers.ToDictionary(h => h.key, h => h.value)
			);
			loader.SetShaderForMaterialType(GLTFLoader.MaterialType.PbrMetallicRoughness, GLTFStandard);
			loader.SetShaderForMaterialType(GLTFLoader.MaterialType.CommonConstant, GLTFConstant);
			loader.Multithreaded = Multithreaded;
			loader.MaximumLod = MaximumLod;
			yield return loader.Load();
		}
	}
}
