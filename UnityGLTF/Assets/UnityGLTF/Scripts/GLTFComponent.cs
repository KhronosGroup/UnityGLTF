using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF {

	/// <summary>
	/// Component to load a GLTF scene with
	/// </summary>
	public class GLTFComponent : MonoBehaviour
	{
		public string Url;
		public bool Multithreaded = true;
		public bool UseStream = false;

		public int MaximumLod = 300;

		public Shader GLTFStandard = null;
		public Shader GLTFConstant = null;

		IEnumerator Start()
		{
			ILoader loader = null;
			GLTFSceneImporter importer = null;
			FileStream gltfStream = null;
			if (UseStream)
			{
				var fullPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + Url;
				gltfStream = File.OpenRead(fullPath);
				var gltfRoot = GLTF.GLTFParser.ParseJson(gltfStream);
				var fileName = Path.GetFileName (fullPath);
				loader = new FileLoader(fullPath.Substring(0, fullPath.Length - fileName.Length));
				
				importer = new GLTFSceneImporter(
					gltfRoot,
					loader,
					gltfStream
					);
			}
			else
			{
				Uri uri = new Uri(Url);
				var directoryPath = URIHelper.AbsoluteUriPath(uri);
				loader = new WebRequestLoader(directoryPath);
				importer = new GLTFSceneImporter(
					URIHelper.GetFileFromUri(uri),
					loader
					);

				importer.SceneParent = gameObject.transform;
			}

			importer.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandard);
			importer.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.CommonConstant, GLTFConstant);
			importer.MaximumLod = MaximumLod;
			if (gltfStream != null)
			{
				GameObject node = importer.LoadNode(0);
				node.transform.SetParent(gameObject.transform, false);

#if !WINDOWS_UWP
				gltfStream.Close();
#else
				gltfStream.Dispose();
#endif
			}
			else
			{
				yield return importer.LoadScene(-1, Multithreaded);
			}
		}
	}
}
