using System;
using System.Collections;
using System.IO;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF {

	/// <summary>
	/// Component to load a GLTF scene with
	/// </summary>
	public class GLTFComponent : MonoBehaviour
	{
		public string GLTFUri;
		public bool Multithreaded = true;
		public bool UseStream = false;

		public int MaximumLod = 300;

		public Shader GLTFStandard;
		public Shader GLTFStandardSpecular;
		public Shader GLTFConstant;

		public bool addColliders = false;

		IEnumerator Start()
		{
			GLTFSceneImporter sceneImporter = null;
			ILoader loader = null;

			if (UseStream)
			{
				string fullPath = Path.Combine(Application.streamingAssetsPath, GLTFUri);
				string directoryPath = URIHelper.GetDirectoryName(fullPath);
				loader = new FileLoader(directoryPath);
				sceneImporter = new GLTFSceneImporter(
					Path.GetFileName(GLTFUri),
					loader
					);
			}
			else
			{
				string directoryPath = URIHelper.GetDirectoryName(GLTFUri);
				loader = new WebRequestLoader(directoryPath);
				sceneImporter = new GLTFSceneImporter(
					URIHelper.GetFileFromUri(new Uri(GLTFUri)),
					loader
					);

			}

			sceneImporter.SceneParent = gameObject.transform;
			sceneImporter.AddColliders = true;
			sceneImporter.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandard);
			sceneImporter.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.KHR_materials_pbrSpecularGlossiness, GLTFStandardSpecular);
			sceneImporter.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.CommonConstant, GLTFConstant);
			sceneImporter.MaximumLod = MaximumLod;
			yield return sceneImporter.LoadScene(-1, Multithreaded);
		}
	}
}
