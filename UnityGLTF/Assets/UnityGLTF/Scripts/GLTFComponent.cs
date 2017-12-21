using System;
using System.Collections;
using System.IO;
using GLTF.Schema;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif
using UnityEngine;
using UnityGLTF.Loader;
#if WINDOWS_UWP
using Windows.Storage;
#endif

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

		[SerializeField]
		private KeyCode KeyToPress = KeyCode.L;
		private bool didExecute = false;
		private void Update()
		{
			if (Input.GetKeyDown(KeyToPress) && !didExecute)
			{
				didExecute = true;
				StartCoroutine(LoadGLTF());
            }
        }
		
		private IEnumerator LoadGLTF()
		{
			ILoader loader = null;
			GLTFSceneImporter importer = null;
#if !WINDOWS_UWP
			FileStream gltfStream = null;
#endif

			if (UseStream)
			{
#if WINDOWS_UWP
				var objectsLibrary = KnownFolders.Objects3D;
				loader = new StorageFolderLoader(objectsLibrary);
				yield return loader.LoadStream(Path.GetFileName(Url));
				Stream gltfStream = loader.LoadedStream;
				var gltfRoot = GLTF.GLTFParser.ParseJson(gltfStream);

				importer = new GLTFSceneImporter(gltfRoot, loader);
#else
				var fullPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + Url;
				gltfStream = File.OpenRead(fullPath);
				var gltfRoot = GLTF.GLTFParser.ParseJson(gltfStream);
				loader = new FileLoader(URIHelper.GetDirectoryName(fullPath));

				importer = new GLTFSceneImporter(
					gltfRoot,
					loader,
					gltfStream
				);
#endif
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

#if !WINDOWS_UWP
			if (UseStream)
			{
				yield return importer.LoadNode(0);
				GameObject node = importer.CreatedObject;
				node.transform.SetParent(gameObject.transform, false);
				gltfStream.Close();
			}
			else
#endif
			{
#if WINDOWS_UWP
				yield return importer.LoadNode(0);
				GameObject node = importer.CreatedObject;
				node.transform.SetParent(gameObject.transform, false);
#else
				yield return importer.LoadScene(-1, Multithreaded);
#endif
			}
		}
	}
}
