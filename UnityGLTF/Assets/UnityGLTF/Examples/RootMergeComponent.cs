using System.Collections;
using System.IO;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;

namespace UnityGLTF
{
	public class RootMergeComponent : MonoBehaviour
	{
		public string asset0Path;
		public string asset1Path;
		public bool Multithreaded = true;

		public int MaximumLod = 300;

		// todo undo
#if !WINDOWS_UWP
		IEnumerator Start()
		{
			var fullPath0 = Application.streamingAssetsPath + Path.DirectorySeparatorChar + asset0Path;
			ILoader loader0 = new FileLoader(URIHelper.GetDirectoryName(fullPath0));

			var fullPath1 = Application.streamingAssetsPath + Path.DirectorySeparatorChar + asset1Path;
			ILoader loader1 = new FileLoader(URIHelper.GetDirectoryName(fullPath1));

			yield return loader0.LoadStream(Path.GetFileName(asset0Path));
			var asset0Stream = loader0.LoadedStream;
			var asset0Root = GLTFParser.ParseJson(asset0Stream);

			yield return loader1.LoadStream(Path.GetFileName(asset1Path));
			var asset1Stream = loader1.LoadedStream;
			var asset1Root = GLTFParser.ParseJson(asset1Stream);

			string newPath = "../../" + URIHelper.GetDirectoryName(asset0Path);

			int previousBufferCount = asset1Root.Buffers.Count;
			int previousImageCount = asset1Root.Images.Count;
			int previousSceneCounter = asset1Root.Scenes.Count;
			GLTFHelpers.MergeGLTF(asset1Root, asset0Root);

			for (int i = previousBufferCount; i < asset1Root.Buffers.Count; ++i)
			{
				GLTF.Schema.Buffer buffer = asset1Root.Buffers[i];
				if (!URIHelper.IsBase64Uri(buffer.Uri))
				{
					buffer.Uri = newPath + buffer.Uri;
				}
			}

			for (int i = previousImageCount; i < asset1Root.Images.Count; ++i)
			{
				Image image = asset1Root.Images[i];
				if (!URIHelper.IsBase64Uri(image.Uri))
				{
					image.Uri = newPath + image.Uri;
				}
			}

			foreach (NodeId node in asset1Root.Scenes[asset0Root.Scene.Id + previousSceneCounter].Nodes)
			{
				node.Value.Translation.X += 5f;
				asset1Root.Scene.Value.Nodes.Add(node);
			}
			GLTFSceneImporter importer = new GLTFSceneImporter(
				asset1Root,
				loader1
				);

			importer.MaximumLod = MaximumLod;

			yield return importer.LoadScene(-1, Multithreaded);
		}
#endif
	}
}
