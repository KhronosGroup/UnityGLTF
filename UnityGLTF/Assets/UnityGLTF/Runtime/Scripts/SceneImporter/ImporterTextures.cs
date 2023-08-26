using System;
using System.IO;
using System.Threading.Tasks;
using GLTF.Schema;
using GLTF.Utilities;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		internal const string EMPTY_TEXTURE_NAME_SUFFIX = " \0";

		private class TextureData
		{
			public Texture Texture = null;
			public int TexCoord = 0;
			public double DataMultiplier = 1;
			public Vector2 Offset = Vector2.zero;
			public double Rotation = 0;
			public Vector2 Scale = Vector2.one;
			public Nullable<int> TexCoordExtra = 0;
		}

		private async Task CreateNotReferencedTexture(int index)
		{
			if (Root.Textures[index].Source != null
			    && Root.Images.Count > 0
			    && Root.Images.Count > Root.Textures[index].Source.Id
			    && string.IsNullOrEmpty(Root.Textures[index].Source.Value.Uri))
			{
				await ConstructImageBuffer(Root.Textures[index], index);
				await ConstructTexture(Root.Textures[index], index, !KeepCPUCopyOfTexture, true);
			}
		}

		private async Task<TextureData> FromTextureInfo(TextureInfo textureInfo)
		{
			var result = new TextureData();
			if (textureInfo?.Index?.Value == null) return result;

			TextureId textureId = textureInfo.Index;
			await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
			result.Texture = _assetCache.TextureCache[textureId.Id].Texture;
			result.TexCoord = textureInfo.TexCoord;

			if (textureInfo is NormalTextureInfo nti)
				result.DataMultiplier = nti.Scale;
			if (textureInfo is OcclusionTextureInfo oti)
				result.DataMultiplier = oti.Strength;

			var ext = GetTextureTransform(textureInfo);
			if (ext != null)
			{
				result.Offset = ext.Offset.ToUnityVector2Raw();
				result.Rotation = ext.Rotation;
				result.Scale = ext.Scale.ToUnityVector2Raw();
				result.TexCoordExtra = ext.TexCoord;
			}

			return result;
		}

		protected async Task ConstructImage(GLTFImage image, int imageCacheIndex, bool markGpuOnly, bool isLinear)
		{
			if (_assetCache.InvalidImageCache[imageCacheIndex])
				return;

			if (_assetCache.ImageCache[imageCacheIndex] == null)
			{
				Stream stream = null;
				if (image.Uri == null)
				{
					var bufferView = image.BufferView.Value;
					var data = new byte[bufferView.ByteLength];

					BufferCacheData bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
					bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
					stream = new SubStream(bufferContents.Stream, 0, data.Length);
				}
				else
				{
					string uri = image.Uri;

					byte[] bufferData;
					URIHelper.TryParseBase64(uri, out bufferData);
					if (bufferData != null)
					{
						stream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
					}
					else
					{
						stream = _assetCache.ImageStreamCache[imageCacheIndex];
					}
				}

				await YieldOnTimeoutAndThrowOnLowMemory();
				await ConstructUnityTexture(stream, markGpuOnly, isLinear, image, imageCacheIndex);
			}
		}

		protected async Task ConstructImageBuffer(GLTFTexture texture, int textureIndex)
		{
			int sourceId = GetTextureSourceId(texture);
			if (_assetCache.ImageStreamCache[sourceId] == null)
			{
				GLTFImage image = _gltfRoot.Images[sourceId];

				// we only load the streams if not a base64 uri, meaning the data is in the uri
				if (image.Uri != null && !URIHelper.IsBase64Uri(image.Uri))
				{
					_assetCache.ImageStreamCache[sourceId] = await _options.DataLoader.LoadStreamAsync(image.Uri);
				}
				else if (image.Uri == null && image.BufferView != null && _assetCache.BufferCache[image.BufferView.Value.Buffer.Id] == null)
				{
					int bufferIndex = image.BufferView.Value.Buffer.Id;
					await ConstructBuffer(_gltfRoot.Buffers[bufferIndex], bufferIndex);
				}
			}

			if (_assetCache.TextureCache[textureIndex] == null)
			{
				_assetCache.TextureCache[textureIndex] = new TextureCacheData
				{
					TextureDefinition = texture
				};
			}
		}

		// With using KTX, we need to return a new Texture2D instance at the moment. Unity KTX package does not support loading into existing one
		async Task<Texture2D> CheckMimeTypeAndLoadImage(GLTFImage image, Texture2D texture, byte[] data, bool markGpuOnly)
		{
			switch (image.MimeType)
			{
				case "image/png":
				case "image/jpeg":
					//	NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
					texture.LoadImage(data, markGpuOnly);
					break;
				case "image/exr":
					Debug.Log(LogType.Warning, $"EXR images are not supported. The texture {texture.name} won't be imported. glTF filename: {_gltfFileName}");
					break;
				case "image/ktx2":
					string textureName = texture.name;
#if HAVE_KTX
#if UNITY_EDITOR
					Texture.DestroyImmediate(texture);
#else
					Texture.Destroy(texture);
#endif
					var ktxTexture = new KtxUnity.KtxTexture();

					using (var alloc = new Unity.Collections.NativeArray<byte>(data, Unity.Collections.Allocator.Persistent))
					{
						var resultTextureData = await ktxTexture.LoadFromBytes(alloc, false);
						texture = resultTextureData.texture;
						texture.name = textureName;
					}

					ktxTexture.Dispose();
#else
					Debug.Log(LogType.Warning, $"Can't import texture \"{image.Name}\" from \"{_gltfFileName}\" because it is a KTX2 file using the KHR_texture_basisu extension. Please add the package \"com.atteneder.ktx\" version v1.3+ to your project to import KTX2 textures.");
					await Task.CompletedTask;
					texture = null;
#endif
					break;
				default:
					texture.LoadImage(data, markGpuOnly);
					break;
			}

			// assign default values
			if (texture)
			{
				texture.wrapMode = TextureWrapMode.Repeat;
				texture.wrapModeV = TextureWrapMode.Repeat;
				texture.wrapModeU = TextureWrapMode.Repeat;
				texture.filterMode = FilterMode.Bilinear;
			}

			await Task.CompletedTask;
			return texture;
		}

		protected virtual async Task ConstructUnityTexture(Stream stream, bool markGpuOnly, bool isLinear, GLTFImage image, int imageCacheIndex)
		{
#if UNITY_EDITOR
			if (stream is AssetDatabaseStream assetDatabaseStream)
			{
				var tx = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetDatabaseStream.AssetUri);
				progressStatus.TextureLoaded++;
				progress?.Report(progressStatus);
				_assetCache.ImageCache[imageCacheIndex] = tx;
				return;
			}
#endif
			Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, GenerateMipMapsForTextures, isLinear);
			texture.name = string.IsNullOrEmpty(image.Name) ? Path.GetFileNameWithoutExtension(image.Uri) : image.Name;
			if (string.IsNullOrEmpty(texture.name))
				texture.name = $"Texture_{imageCacheIndex.ToString()}{EMPTY_TEXTURE_NAME_SUFFIX}";

			var newTextureObject = texture;

#if UNITY_EDITOR
			var haveRemappedTexture = false;
			if (Context.SourceImporter != null)
			{
				// check for remapping, we don't even need to attempt loading the texture in that case.
				var externalObjects = Context.SourceImporter.GetExternalObjectMap();
				var sourceIdentifier = new AssetImporter.SourceAssetIdentifier(texture);
				externalObjects.TryGetValue(sourceIdentifier, out var o);
				if (o is Texture2D remappedTexture)
				{
					if (remappedTexture)
					{
						texture = remappedTexture;
						haveRemappedTexture = true;
					}
					else
					{
						Context.SourceImporter.RemoveRemap(sourceIdentifier);
					}
				}
			}

			if (haveRemappedTexture)
			{
				// nothing to do here, the texture has already been remapped
			}
			else
#endif
			if (stream is FileLoader.InvalidStream invalidStream)
			{
				// ignore - we still need a valid texture so that we can properly remap
				// texture = null;
				// We need some way to track these mock textures, so that we can get rid of them again after import
				// TODO we should still set it to null here, and save the importer definition names for remapping instead.
				// This way here we'll get into weird code for Runtime import, as we would still import mock textures...
				// Or we add another option to avoid that.
				texture = null;
				UnityEngine.Debug.LogError("Buffer file " + invalidStream.RelativeFilePath + " not found in path: " + invalidStream.AbsoluteFilePath);

			}
			else if (stream is MemoryStream)
			{
				using (MemoryStream memoryStream = stream as MemoryStream)
				{
					await YieldOnTimeoutAndThrowOnLowMemory();
					texture = await CheckMimeTypeAndLoadImage(image, texture, memoryStream.ToArray(), markGpuOnly);
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

				await YieldOnTimeoutAndThrowOnLowMemory();
				texture = await CheckMimeTypeAndLoadImage(image, texture, buffer, markGpuOnly);
			}

			if (!texture)
				_assetCache.InvalidImageCache[imageCacheIndex] = newTextureObject;

			if (_assetCache.ImageCache[imageCacheIndex] != null) Debug.Log(LogType.Assert, "ImageCache should not be loaded multiple times");
			if (texture)
			{
				progressStatus.TextureLoaded++;
				progress?.Report(progressStatus);
			}
			_assetCache.ImageCache[imageCacheIndex] = texture;
		}


		protected virtual int GetTextureSourceId(GLTFTexture texture)
		{
			if (texture.Extensions != null && texture.Extensions.ContainsKey(KHR_texture_basisu.EXTENSION_NAME))
			{
				return ((KHR_texture_basisu)texture.Extensions[KHR_texture_basisu.EXTENSION_NAME]).source.Id;
			}
			return texture.Source?.Id ?? 0;
		}

		protected virtual bool IsTextureFlipped(GLTFTexture texture)
		{
			if (texture.Extensions != null && texture.Extensions.ContainsKey(KHR_texture_basisu.EXTENSION_NAME))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Creates a texture from a glTF texture
		/// </summary>
		/// <param name="texture">The texture to load</param>
		/// <param name="textureIndex">The index in the texture cache</param>
		/// <param name="markGpuOnly">Whether the texture is GPU only, instead of keeping a CPU copy</param>
		/// <param name="isLinear">Whether the texture is linear rather than sRGB</param>
		/// <returns>The loading task</returns>
		public virtual async Task LoadTextureAsync(GLTFTexture texture, int textureIndex, bool markGpuOnly, bool isLinear)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException("Cannot CreateTexture while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				if (_options.ThrowOnLowMemory)
				{
					_memoryChecker = new MemoryChecker();
				}

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
				}

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				await ConstructImageBuffer(texture, textureIndex);
				await ConstructTexture(texture, textureIndex, markGpuOnly, isLinear);
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
				_gltfStream.Stream.Close();
			}
		}

		public virtual Task LoadTextureAsync(GLTFTexture texture, int textureIndex, bool isLinear)
		{
			return LoadTextureAsync(texture, textureIndex, !KeepCPUCopyOfTexture, isLinear);
		}

		/// <summary>
		/// Gets texture that has been loaded from CreateTexture
		/// </summary>
		/// <param name="textureIndex">The texture to get</param>
		/// <returns>Created texture</returns>
		public virtual Texture GetTexture(int textureIndex)
		{
			if (_assetCache == null)
			{
				throw new GLTFLoadException("Asset cache needs initialized before calling GetTexture");
			}

			if (_assetCache.TextureCache[textureIndex] == null)
			{
				return null;
			}

			return _assetCache.TextureCache[textureIndex].Texture;
		}

		protected virtual async Task ConstructTexture(GLTFTexture texture, int textureIndex, bool markGpuOnly, bool isLinear)
		{
			if (_assetCache.TextureCache[textureIndex].Texture == null)
			{
				int sourceId = GetTextureSourceId(texture);
				GLTFImage image = _gltfRoot.Images[sourceId];
				await ConstructImage(image, sourceId, markGpuOnly, isLinear);

				var source = _assetCache.ImageCache[sourceId];
				if (!source) return;

				FilterMode desiredFilterMode;
				TextureWrapMode desiredWrapModeS, desiredWrapModeT;

				if (texture.Sampler != null)
				{
					var sampler = texture.Sampler.Value;
					switch (sampler.MinFilter)
					{
						case MinFilterMode.Nearest:
						case MinFilterMode.NearestMipmapNearest:
						case MinFilterMode.NearestMipmapLinear:
							desiredFilterMode = FilterMode.Point;
							break;
						case MinFilterMode.Linear:
						case MinFilterMode.LinearMipmapNearest:
							desiredFilterMode = FilterMode.Bilinear;
							break;
						case MinFilterMode.LinearMipmapLinear:
							desiredFilterMode = FilterMode.Trilinear;
							break;
						default:
							Debug.Log(LogType.Warning, "Unsupported Sampler.MinFilter: " + sampler.MinFilter);
							desiredFilterMode = FilterMode.Trilinear;
							break;
					}

					TextureWrapMode UnityWrapMode(GLTF.Schema.WrapMode gltfWrapMode)
					{
						switch (gltfWrapMode)
						{
							case GLTF.Schema.WrapMode.ClampToEdge:
								return TextureWrapMode.Clamp;
							case GLTF.Schema.WrapMode.Repeat:
								return TextureWrapMode.Repeat;
							case GLTF.Schema.WrapMode.MirroredRepeat:
								return TextureWrapMode.Mirror;
							default:
								Debug.Log(LogType.Warning, "Unsupported Sampler.Wrap: " + gltfWrapMode);
								return TextureWrapMode.Repeat;
						}
					}

					desiredWrapModeS = UnityWrapMode(sampler.WrapS);
					desiredWrapModeT = UnityWrapMode(sampler.WrapT);
				}
				else
				{
					desiredFilterMode = FilterMode.Trilinear;
					desiredWrapModeS = TextureWrapMode.Repeat;
					desiredWrapModeT = TextureWrapMode.Repeat;
				}

				var matchSamplerState = source.filterMode == desiredFilterMode && source.wrapModeU == desiredWrapModeS && source.wrapModeV == desiredWrapModeT;
				if (matchSamplerState || markGpuOnly)
				{
					if (_assetCache.TextureCache[textureIndex].Texture != null) Debug.Log(LogType.Assert, "Texture should not be reset to prevent memory leaks");
					_assetCache.TextureCache[textureIndex].Texture = source;

					if (!matchSamplerState)
					{
						Debug.Log(LogType.Warning, $"Ignoring sampler; filter mode: source {source.filterMode}, desired {desiredFilterMode}; wrap mode: source {source.wrapModeU}x{source.wrapModeV}, desired {desiredWrapModeS}x{desiredWrapModeT}");
					}
				}
				else
#if UNITY_EDITOR
				if (!UnityEditor.AssetDatabase.Contains(source))
#endif
				{
					Texture2D unityTexture;
					if (!source.isReadable)
					{
						unityTexture = new Texture2D(source.width, source.height, source.format, source.mipmapCount, isLinear);
						Graphics.CopyTexture(source, unityTexture);
					}
					else
						unityTexture = Object.Instantiate(source);

					unityTexture.name = string.IsNullOrEmpty(image.Name) ?
						string.IsNullOrEmpty(texture.Name) ?
							Path.GetFileNameWithoutExtension(image.Uri) :
							texture.Name :
						image.Name;

					if (string.IsNullOrEmpty(unityTexture.name))
						unityTexture.name = $"Texture_{textureIndex.ToString()}{EMPTY_TEXTURE_NAME_SUFFIX}";

					unityTexture.filterMode = desiredFilterMode;
					unityTexture.wrapModeU = desiredWrapModeS;
					unityTexture.wrapModeV = desiredWrapModeT;

					if (_assetCache.TextureCache[textureIndex].Texture != null) Debug.Log(LogType.Assert, "Texture should not be reset to prevent memory leaks");
					_assetCache.TextureCache[textureIndex].Texture = unityTexture;
				}
#if UNITY_EDITOR
				else
				{
					// don't warn for just filter mode, user choice
					if (source.wrapModeU != desiredWrapModeS || source.wrapModeV != desiredWrapModeT)
						Debug.Log(LogType.Warning, ($"Sampler state doesn't match but source texture is non-readable. Results might not be correct if textures are used multiple times with different sampler states. {source.filterMode} == {desiredFilterMode} && {source.wrapModeU} == {desiredWrapModeS} && {source.wrapModeV} == {desiredWrapModeT}"));
					_assetCache.TextureCache[textureIndex].Texture = source;
				}
#endif
			}

			_assetCache.TextureCache[textureIndex].IsLinear = isLinear;

			try
			{
				var tex = _assetCache.TextureCache[textureIndex].Texture;

				foreach (var plugin in Context.Plugins)
				{
					plugin.OnAfterImportTexture(texture, textureIndex, tex);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		protected virtual void ConstructImageFromGLB(GLTFImage image, int imageCacheIndex)
		{
			var texture = new Texture2D(0, 0);
			texture.name = nameof(GLTFSceneImporter) + (image.Name != null ? ("." + image.Name) : "");
			var bufferView = image.BufferView.Value;
			var data = new byte[bufferView.ByteLength];

			var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
			bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
			bufferContents.Stream.Read(data, 0, data.Length);
			texture.LoadImage(data);

			if (_assetCache.ImageCache[imageCacheIndex] != null) Debug.Log(LogType.Assert, "ImageCache should not be loaded multiple times");
			progressStatus.TextureLoaded++;
			progress?.Report(progressStatus);
			_assetCache.ImageCache[imageCacheIndex] = texture;
		}

#if UNITY_EDITOR
		internal class AssetDatabaseStream : Stream
		{
			public string AssetUri { get; }

			public AssetDatabaseStream(string imageUri)
			{
				AssetUri = imageUri;
			}

			public override void Flush() => throw new NotImplementedException();
			public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
			public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
			public override void SetLength(long value) => throw new NotImplementedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

			public override bool CanRead { get; }
			public override bool CanSeek { get; }
			public override bool CanWrite { get; }
			public override long Length { get; }
			public override long Position { get; set; }
		}
#endif
	}

	internal static class MatHelper
	{
		internal static void SetKeyword(this Material material, string keyword, bool state)
		{
			if (state)
				material.EnableKeyword(keyword + "_ON");
			else
				material.DisableKeyword(keyword + "_ON");

			if (material.HasProperty(keyword))
				material.SetFloat(keyword, state ? 1 : 0);
		}
	}
}
