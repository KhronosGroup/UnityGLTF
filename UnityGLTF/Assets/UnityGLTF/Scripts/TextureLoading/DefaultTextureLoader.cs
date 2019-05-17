using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF
{
	class DefaultTextureLoader : ITextureLoader
	{
		public bool CanLoadTexture(GLTFImage image)
		{
			//TODO: should this actually list out what Unity natively knows how to handle?
			return true;
		}

		public async Task<Texture2D> LoadTextureAsync(Stream stream, bool markGpuOnly, bool isLinear, GLTFImage image, AsyncCoroutineHelper asyncCoroutineHelper)
		{
			Texture2D texture = new Texture2D(0, 0, TextureFormat.RGBA32, true, isLinear);
			texture.name = nameof(GLTFSceneImporter) + (image.Name != null ? ("." + image.Name) : "");

			if (stream is MemoryStream)
			{
				using (MemoryStream memoryStream = stream as MemoryStream)
				{
					texture.LoadImage(memoryStream.ToArray(), markGpuOnly);
				}
			}
			else
			{
				byte[] buffer = new byte[stream.Length];

				// todo: potential optimization is to split stream read into multiple frames (or put it on a thread?)
				if (stream.Length > int.MaxValue)
				{
					throw new Exception("Stream is larger than can be copied into byte array");
				}
				stream.Read(buffer, 0, (int)stream.Length);

				if (asyncCoroutineHelper != null) await asyncCoroutineHelper.YieldOnTimeout();
				//	NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
				texture.LoadImage(buffer, markGpuOnly);
			}

			return texture;
		}
	}
}
