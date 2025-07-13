using System;
using System.Collections.Generic;
using System.IO;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Cache;
using Object = UnityEngine.Object;
using WrapMode = GLTF.Schema.WrapMode;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private string GetUniqueName(HashSet<string> existingNames, string name)
		{
			if (!existingNames.Contains(name)) return name;

			string newName = name;
			if (Path.HasExtension(name))
			{
				string extension = Path.GetExtension(name);
				string baseName = Path.GetFileNameWithoutExtension(name);
				var i = 1;
				newName = $"{baseName}-{i}{extension}";
				while (existingNames.Contains($"{baseName}-{i}{extension}"))
				{
					i++;
					newName = $"{baseName}-{i}{extension}";
				}
			}
			else
			{
				var i = 1;
				newName = $"{name}-{i}";
				while (existingNames.Contains($"{name}-{i}"))
				{
					i++;
					newName = $"{name}-{i}";
				}
			}
			
			return newName;
		}

		private void ExportImages(string outputPath)
		{
			for (int t = 0; t < _imageInfos.Count; ++t)
			{
				writeImageToDiskMarker.Begin();

				var image = _imageInfos[t].texture;
				var textureMapType = _imageInfos[t].textureMapType;
				var fileOutputPath = Path.Combine(outputPath, _imageInfos[t].outputPath);
				_imageExportPaths.Add(fileOutputPath);

				var canBeExportedFromDisk = _imageInfos[t].canBeExportedFromDisk;

				var dir = Path.GetDirectoryName(fileOutputPath);
				if (!Directory.Exists(dir) && dir != null)
					Directory.CreateDirectory(dir);

				bool wasAbleToExportTexture = false;
				if (canBeExportedFromDisk)
				{
					File.WriteAllBytes(fileOutputPath, GetTextureDataFromDisk(image));
					wasAbleToExportTexture = true;
				}

				if (!wasAbleToExportTexture)
				{
					switch (textureMapType.conversion)
					{
						case TextureExportSettings.Conversion.MetalGlossChannelSwap:
							GetConversionMaterial(textureMapType); // Sets paramaters to the material
							ExportLinearTexture(image, fileOutputPath, true);
							break;
						case TextureExportSettings.Conversion.MetalGlossOcclusionChannelSwap:
							GetConversionMaterial(textureMapType); // Sets paramaters to the material
							ExportLinearTexture(image, fileOutputPath, true, true);
							break;
						case TextureExportSettings.Conversion.None:
							if (textureMapType.linear)
								ExportLinearTexture(image, fileOutputPath, false);
							else
								ExportTexture(image, fileOutputPath);
							break;
						case TextureExportSettings.Conversion.NormalChannel:
							ExportNormalTexture(image, fileOutputPath);
							break;
						default:
							ExportTexture(image, fileOutputPath);
							break;
					}
				}

				writeImageToDiskMarker.End();
			}
		}

		/// <summary>
		/// This converts Unity's metallic-gloss texture representation into GLTF's metallic-roughness specifications.
		/// Unity's metallic-gloss A channel (glossiness) is inverted and goes into GLTF's metallic-roughness G channel (roughness).
		/// Unity's metallic-gloss R channel (metallic) goes into GLTF's metallic-roughess B channel.
		/// </summary>
		/// <param name="texture">Unity's metallic-gloss texture to be exported</param>
		/// <param name="outputPath">The location to export the texture</param>
		private void ExportLinearTexture(Texture2D texture, string outputPath, bool swapMetalGlossChannels, bool includeOcclusionInChannels = false)
		{
			if (!texture)
			{
				Debug.Log(LogType.Error, "Can not export missing texture: " + outputPath);
				return;
			}
			var previousSRGBState = GL.sRGBWrite;
			GL.sRGBWrite = false;

			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			if (swapMetalGlossChannels)
			{
		
				if (includeOcclusionInChannels)
					Graphics.Blit(texture, destRenderTexture, _metalGlossOcclusionChannelSwapMaterial);
				else
					Graphics.Blit(texture, destRenderTexture, _metalGlossChannelSwapMaterial);
			}
			else
				Graphics.Blit(texture, destRenderTexture);
			WriteRenderTextureToDiskAndRelease(destRenderTexture, outputPath, true);
			GL.sRGBWrite = previousSRGBState;
		}

		/// <summary>
		/// This export's the normal texture. If a texture is marked as a normal map, the values are stored in the A and G channel.
		/// To output the correct normal texture, the A channel is put into the R channel.
		/// </summary>
		/// <param name="texture">Unity's normal texture to be exported</param>
		/// <param name="outputPath">The location to export the texture</param>
		private void ExportNormalTexture(Texture2D texture, string outputPath)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			var wr = GL.sRGBWrite;
			GL.sRGBWrite = false;
			Graphics.Blit(texture, destRenderTexture, _normalChannelMaterial);
			WriteRenderTextureToDiskAndRelease(destRenderTexture, outputPath, true);
			GL.sRGBWrite = wr;
		}

		private void ExportTexture(Texture2D texture, string outputPath)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			Graphics.Blit(texture, destRenderTexture);
			WriteRenderTextureToDiskAndRelease(destRenderTexture, outputPath, false);
		}

		private void WriteRenderTextureToDiskAndRelease(RenderTexture destRenderTexture, string outputPath, bool linear)
		{
			RenderTexture.active = destRenderTexture;

			TextureFormat format = TextureFormat.ARGB32;
			if (outputPath.EndsWith(".exr")) format = TextureFormat.RGBAFloat;
			
			var exportTexture = new Texture2D(destRenderTexture.width, destRenderTexture.height, format, false, linear);
			exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
			//exportTexture.Apply();

			byte[] binaryData;
			if(outputPath.EndsWith(".jpg")) 
				binaryData = exportTexture.EncodeToJPG(settings.DefaultJpegQuality);
			else if(outputPath.EndsWith(".png"))
				binaryData = exportTexture.EncodeToPNG();
			else if (outputPath.EndsWith(".exr"))
				binaryData = exportTexture.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
			else
			{
				Debug.LogError("Unsupported file extension: " + outputPath, destRenderTexture);
				binaryData = exportTexture.EncodeToPNG();
			}
			
			File.WriteAllBytes(outputPath, binaryData);

			RenderTexture.ReleaseTemporary(destRenderTexture);
			if (Application.isEditor)
				Object.DestroyImmediate(exportTexture);
			else
				Object.Destroy(exportTexture);
		}

		public TextureInfo ExportTextureInfo(Texture texture, string textureSlot, TextureExportSettings exportSettings = default)
		{
			if (!texture) return null;

			var info = new TextureInfo();

			info.Index = ExportTexture(texture, textureSlot, exportSettings);

			return info;
		}

		public TextureInfo ExportTextureInfoWithTextureTransform(Material mat, Texture texture, string textureSlot,
			TextureExportSettings exportSettings = default)
		{
			var tInfo = ExportTextureInfo(texture, textureSlot, exportSettings);
			if (texture != null)
				ExportTextureTransform(tInfo, mat, textureSlot);
			return tInfo;
		}

		public TextureId ExportTexture(Texture textureObj, string textureSlot, TextureExportSettings exportSettings = default)
		{
			var uniqueTexture = exportSettings.isValid ?
				new UniqueTexture(textureObj, exportSettings) :
				new UniqueTexture(textureObj, textureSlot, this);

			foreach (var plugin in _plugins)
				plugin?.BeforeTextureExport(this, ref uniqueTexture, textureSlot);

			TextureId id = GetTextureId(_root, uniqueTexture);
			if (id != null)
			{
				return id;
			}

			var texture = new GLTFTexture();
			if (_root.Textures == null) _root.Textures = new List<GLTFTexture>();

			//If texture name not set give it a unique name using count
			if (textureObj.name == "")
			{
				textureObj.name = (_root.Textures.Count + 1).ToString();
			}

			if (settings.ExportNames)
			{
				texture.Name = textureObj.name;
			}

			if (_shouldUseInternalBufferForImages)
		    {
				texture.Source = ExportImageInternalBuffer(uniqueTexture, textureSlot);
		    }
		    else
		    {
				texture.Source = ExportImage(uniqueTexture, textureSlot);
		    }
			texture.Sampler = ExportSampler(textureObj);

			_textures.Add(uniqueTexture);

			id = new TextureId
			{
				Id = _root.Textures.Count,
				Root = _root
			};

			_root.Textures.Add(texture);

			// HACK we should properly check if the above texture was exported as EXR and then treat it differently;
			// this here assumes the texture is already readable / was read back
			if (textureSlot == TextureMapType.Custom_HDR)
			{
				if (texture.Extensions == null) texture.Extensions = new Dictionary<string, IExtension>();
				texture.Extensions.Add(EXT_texture_exr.EXTENSION_NAME, new EXT_texture_exr(texture.Source));
				DeclareExtensionUsage(EXT_texture_exr.EXTENSION_NAME);
			}

			foreach (var plugin in _plugins)
				plugin?.AfterTextureExport(this, uniqueTexture, id.Id, texture);

			return id;
		}

		/// <summary>
		/// Used for determining the resulting file output path for external images (not internal buffer).
		/// Depends on various factors such as alpha channel, texture map type / conversion needs, and existing data on disk.
		/// Logic needs to match the one in ExportImageInternalBuffer.
		/// The actual export happens in ExportImages.
		/// </summary>
		/// <returns>The relative texture output path on disk, including extension</returns>
		private string GetImageOutputPath(Texture texture, TextureExportSettings textureMapType, string textureSlot, out bool ableToExportFromDisk)
		{
			var imagePath = _exportContext.TexturePathRetriever(texture);
			if (string.IsNullOrEmpty(imagePath))
			{
				imagePath = texture.name;
			}

			ableToExportFromDisk = false;
			bool textureHasAlpha = true;

			if (settings.TryExportTexturesFromDisk && CanGetTextureDataFromDisk(textureMapType, texture, out string path))
			{
				if (IsPng(path) || IsJpeg(path))
				{
					imagePath = path;
					ableToExportFromDisk = true;
				}
			}

			switch (textureMapType.alphaMode)
			{
				case TextureExportSettings.AlphaMode.Never:
					textureHasAlpha = false;
					break;
				case TextureExportSettings.AlphaMode.Heuristic:
					textureHasAlpha = TextureHasAlphaChannel(texture);
					break;
			}

			var canExportAsJpeg = !textureHasAlpha && settings.UseTextureFileTypeHeuristic;
			var desiredExtension = canExportAsJpeg ? ".jpg" : ".png";
			if (textureSlot == TextureMapType.Custom_HDR)
				desiredExtension = ".exr";

			if (!settings.ExportFullPath)
			{
				imagePath = Path.GetFileName(imagePath);
			}

			if (!ableToExportFromDisk)
			{
				imagePath = Path.ChangeExtension(imagePath, desiredExtension);
			}
			
			// Avoid overwriting existing files
			imagePath = GetUniqueName(_imageExportPaths, imagePath);
			_imageExportPaths.Add(imagePath);

			return imagePath;
		}

		private ImageId ExportImage(UniqueTexture uniqueTexture, string textureSlot)
		{
			var texture = uniqueTexture.Texture;
			var width = uniqueTexture.GetWidth();
			var height = uniqueTexture.GetHeight();

			ImageId id = GetImageId(_root, texture, uniqueTexture.ExportSettings);
			if (id != null)
			{
				return id;
			}

			var image = new GLTFImage();

			if (ExportNames)
			{
				image.Name = texture.name;
			}

            if (texture.GetType() == typeof(RenderTexture))
            {
                Texture2D tempTexture = new Texture2D(width, height);
                tempTexture.name = texture.name;

                RenderTexture.active = texture as RenderTexture;
                tempTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tempTexture.Apply();
                texture = tempTexture;
            }

#if UNITY_2017_1_OR_NEWER
            if (texture.GetType() == typeof(CustomRenderTexture))
            {
                Texture2D tempTexture = new Texture2D(width, height);
                tempTexture.name = texture.name;

                RenderTexture.active = texture as CustomRenderTexture;
                tempTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tempTexture.Apply();
                texture = tempTexture;
            }
#endif

			var filenamePath = GetImageOutputPath(texture, uniqueTexture.ExportSettings, textureSlot, out var canBeExportedFromDisk);

			// some characters such as # are allowed as part of an URI and are thus not escaped
			// by EscapeUriString. They need to be escaped if they're part of the filename though.
			image.Uri = Path.Combine(
				Uri.EscapeUriString(Path.GetDirectoryName(filenamePath).Replace("\\","/")),
				Uri.EscapeDataString(Path.GetFileName(filenamePath))
			).Replace("\\","/");

			// TODO: handle cubemap export
			var imageInfoTexture = texture as Texture2D;
            _imageInfos.Add(new ImageInfo
			{
				texture = imageInfoTexture,
				textureMapType = uniqueTexture.ExportSettings,
				outputPath = filenamePath,
				canBeExportedFromDisk = canBeExportedFromDisk,
			});

            id = new ImageId
			{
				Id = _root.Images.Count,
				Root = _root
			};

            if (image.Name.EndsWith(GLTFSceneImporter.EMPTY_TEXTURE_NAME_SUFFIX))
	            image.Name = "";

			_root.Images.Add(image);

			return id;
		}

		private bool CanGetTextureDataFromDisk(TextureExportSettings textureMapType, Texture texture, out string path)
		{
			path = null;

#if UNITY_EDITOR
			if (Application.isEditor && UnityEditor.AssetDatabase.Contains(texture))
			{
				path = UnityEditor.AssetDatabase.GetAssetPath(texture);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (importer?.textureShape != TextureImporterShape.Texture2D)
					return false;

				switch (textureMapType.conversion)
				{
					// if this is a normal map generated from greyscale, we shouldn't attempt to export from disk
					case TextureExportSettings.Conversion.NormalChannel:
						if (importer && importer.textureType == TextureImporterType.NormalMap && importer.convertToNormalmap)
							return false;
						break;
					// check if the texture contains an alpha channel; if yes, we shouldn't attempt to export from disk but instead convert.
					case TextureExportSettings.Conversion.MetalGlossChannelSwap:
					case TextureExportSettings.Conversion.MetalGlossOcclusionChannelSwap:
						if (importer && importer.DoesSourceTextureHaveAlpha())
							return false;
						break;
				}

				if (File.Exists(path))
				{
					if(AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Texture2D))
					{
						var ext = Path.GetExtension(path);
						path = path.Replace(ext, "-" + texture.name + ext);
					}
					return true;
				}
			}
#endif
			return false;
		}

		private byte[] GetTextureDataFromDisk(Texture texture)
		{
#if UNITY_EDITOR
			var path = UnityEditor.AssetDatabase.GetAssetPath(texture);

			if (File.Exists(path))
				return File.ReadAllBytes(path);
#endif
			return null;
		}

		bool TextureHasAlphaChannel(Texture sourceTexture)
		{
			var hasAlpha = false;

#if UNITY_EDITOR
			if (AssetDatabase.Contains(sourceTexture) && sourceTexture is Texture2D)
			{
				var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sourceTexture)) as TextureImporter;
				if (importer)
				{
					switch (importer.alphaSource)
					{
						case TextureImporterAlphaSource.FromInput:
							hasAlpha = importer.DoesSourceTextureHaveAlpha();
							break;
						case TextureImporterAlphaSource.FromGrayScale:
							hasAlpha = true;
							break;
						case TextureImporterAlphaSource.None:
							hasAlpha = false;
							break;
					}
				}
			}
#endif

			UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
#if !UNITY_2019_1_OR_NEWER
			if (sourceTexture is Texture2D tex2D)
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(tex2D.format, true);
#else
			graphicsFormat = sourceTexture.graphicsFormat;
#endif
#if UNITY_2018_2_OR_NEWER
			if(graphicsFormat != UnityEngine.Experimental.Rendering.GraphicsFormat.None)
				hasAlpha |= UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(graphicsFormat);
#else
			hasAlpha = true;
#endif
			return hasAlpha;
		}

		private bool IsPng(string filename)
		{
			return Path.GetExtension(filename).EndsWith("png", StringComparison.InvariantCultureIgnoreCase);
		}

		private bool IsJpeg(string filename)
		{
			return Path.GetExtension(filename).EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || Path.GetExtension(filename).EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase);
		}

#if UNITY_EDITOR
		private bool TryGetImporter<T>(Object obj, out T importer) where T : AssetImporter
		{
			if (EditorUtility.IsPersistent(obj))
			{
				var texturePath = AssetDatabase.GetAssetPath(obj);
				importer = AssetImporter.GetAtPath(texturePath) as T;
				return importer;
			}
			importer = null;
			return false;
		}
#endif

		private ImageId ExportImageInternalBuffer(UniqueTexture uniqueTexture, string textureSlot)
		{
			var texture = uniqueTexture.Texture;
			var exportSettings = uniqueTexture.ExportSettings;

			const string PNGMimeType = "image/png";
			const string JPEGMimeType = "image/jpeg";
			const string EXRMimeType = "image/exr";

			if (texture == null)
		    {
				throw new NullReferenceException("texture can not be NULL.");
		    }

		    var image = new GLTFImage();
		    image.MimeType = PNGMimeType;

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			bool wasAbleToExport = false;
			bool textureHasAlpha = true;

			if (settings.TryExportTexturesFromDisk && CanGetTextureDataFromDisk(exportSettings, texture, out string path))
			{
				if (IsPng(path))
				{
					image.MimeType = PNGMimeType;
					var imageBytes = GetTextureDataFromDisk(texture);
					_bufferWriter.Write(imageBytes);
					wasAbleToExport = true;
				}
				else if (IsJpeg(path))
				{
					image.MimeType = JPEGMimeType;
					var imageBytes = GetTextureDataFromDisk(texture);
					_bufferWriter.Write(imageBytes);
					wasAbleToExport = true;
				}
				else
				{
					Debug.Log("Texture can't be exported from disk: " + path + ". Only PNG & JPEG are supported. The texture will be re-encoded as PNG.", texture);
				}
			}

			// export in-memory floating point textures as EXR
			// TODO add readback when not readable
			if (!wasAbleToExport && textureSlot == TextureMapType.Custom_HDR)
			{
				byte[] exrImageData = default;

				if (texture.isReadable)
				{
					const Texture2D.EXRFlags EXR_Flags = Texture2D.EXRFlags.CompressZIP;

					if (texture is Texture2D texture2D)
					{
						exrImageData = texture2D.EncodeToEXR(EXR_Flags);
					}
					else if (texture is RenderTexture rt)
					{
						var prev = RenderTexture.active;
						RenderTexture.active = rt;
						var temp = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false, true);
						temp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
						temp.Apply();
						RenderTexture.active = prev;
						exrImageData = temp.EncodeToEXR(EXR_Flags);
					}
				}

				if (exrImageData != null)
				{
					image.MimeType = EXRMimeType;
					_bufferWriter.Write(exrImageData);
					wasAbleToExport = true;
				}
			}

			if (!wasAbleToExport)
		    {
			    var sRGB = !exportSettings.linear;

#if UNITY_EDITOR
				if (textureSlot == TextureMapType.Custom_Unknown)
				{
#if UNITY_EDITOR
					if (TryGetImporter<TextureImporter>(texture, out var importer))
					{
						if (!importer.sRGBTexture)
							sRGB = false;
						else
							sRGB = true;
					}
#endif
				}
#endif

				var format = sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;

				var width = uniqueTexture.GetWidth();
				var height = uniqueTexture.GetHeight();

				// TODO we could make sure texture size is power-of-two here

				switch (exportSettings.alphaMode)
				{
					case TextureExportSettings.AlphaMode.Never:
						textureHasAlpha = false;
						break;
					case TextureExportSettings.AlphaMode.Always:
						textureHasAlpha = true;
						break;
					case TextureExportSettings.AlphaMode.Heuristic:
						textureHasAlpha = TextureHasAlphaChannel(texture);
						break;
				}

				if (textureSlot == TextureMapType.Custom_HDR)
				{
					Debug.LogWarning("HDR Texture Export from non-readable textures isn't supported yet", texture);
				}


				var canExportAsJpeg = !textureHasAlpha && settings.UseTextureFileTypeHeuristic;
				image.MimeType = canExportAsJpeg ? JPEGMimeType : PNGMimeType;

				var cacheKey = uniqueTexture.GetHashCode().ToString();
				if (settings.UseCaching && ExportCache.TryGetBytes(texture, cacheKey, out var bytes))
				{
					_bufferWriter.Write(bytes);
				}
				else
				{
					var destRenderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default, format);
					var previousSRGBState = GL.sRGBWrite;
					GL.sRGBWrite = sRGB;

					var shader = GetConversionMaterial(exportSettings);
		
					if (shader)
						Graphics.Blit(texture, destRenderTexture, shader);
					else
						Graphics.Blit(texture, destRenderTexture);

					var exportTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, !sRGB);
					exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);

					var imageData = canExportAsJpeg ? exportTexture.EncodeToJPG(settings.DefaultJpegQuality) : exportTexture.EncodeToPNG();
					_bufferWriter.Write(imageData);

					if (settings.UseCaching)
						ExportCache.AddBytes(texture, cacheKey, imageData);

					GL.sRGBWrite = previousSRGBState;

					RenderTexture.ReleaseTemporary(destRenderTexture);

					if (Application.isEditor)
					{
						UnityEngine.Object.DestroyImmediate(exportTexture);
					}
					else
					{
						UnityEngine.Object.Destroy(exportTexture);
					}
				}


		    }

			// // Check for potential warnings in GLTF validation
			// if (!Mathf.IsPowerOfTwo(texture.width) || !Mathf.IsPowerOfTwo(texture.height))
			// {
			// 	Debug.LogWarning("Validation Warning: " + "Image has non-power-of-two dimensions: " + texture.width + "x" + texture.height + ".", texture);
			// }

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);
			image.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			if (ExportNames)
			{
				image.Name = texture.name;
			}

			if (image.Name.EndsWith(GLTFSceneImporter.EMPTY_TEXTURE_NAME_SUFFIX))
				image.Name = "";

		    var id = new ImageId
		    {
				Id = _root.Images.Count,
				Root = _root
		    };
		    _root.Images.Add(image);

		    return id;
		}

		private struct SamplerRelevantTextureData : IEquatable<SamplerRelevantTextureData>
		{
			private readonly TextureWrapMode wrapMode;
			private readonly FilterMode filterMode;
			private readonly bool hasMipmaps;
			private readonly bool hasAniso;
			public bool HasMipmaps => hasMipmaps;
			public bool HasAniso => hasAniso;

			public SamplerRelevantTextureData(Texture texture)
			{
				var mipmapCount = 1;
				var aniso = 1;
#if UNITY_2019_2_OR_NEWER
				mipmapCount = texture.mipmapCount;
				aniso = texture.anisoLevel;
#else
			if (texture is Texture2D tex2D) mipmapCount = tex2D.mipmapCount;
#endif
				wrapMode = texture.wrapMode;
				filterMode = texture.filterMode;
				hasMipmaps = mipmapCount > 1;
				hasAniso = aniso > 0;
			}

			public bool Equals(SamplerRelevantTextureData other)
			{
				return wrapMode == other.wrapMode && filterMode == other.filterMode && hasMipmaps == other.hasMipmaps && hasAniso == other.hasAniso;
			}

			public override bool Equals(object obj)
			{
				return obj is SamplerRelevantTextureData other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (int)wrapMode;
					hashCode = (hashCode * 397) ^ (int)filterMode;
					hashCode = (hashCode * 397) ^ hasMipmaps.GetHashCode();
					hashCode = (hashCode * 397) ^ hasAniso.GetHashCode();
					return hashCode;
				}
			}
		}

		private Dictionary<SamplerRelevantTextureData, int> _textureSettingsToSamplerIndices =
			new Dictionary<SamplerRelevantTextureData, int>();
		private SamplerId ExportSampler(Texture texture)
		{
			var dataForTexture = new SamplerRelevantTextureData(texture);

			if (_textureSettingsToSamplerIndices.TryGetValue(dataForTexture, out var existingSamplerIndex))
			{
				return new SamplerId
				{
					Id = existingSamplerIndex,
					Root = _root
				};
			}

			var sampler = new Sampler();

			switch (texture.wrapMode)
			{
				case TextureWrapMode.Clamp:
					sampler.WrapS = WrapMode.ClampToEdge;
					sampler.WrapT = WrapMode.ClampToEdge;
					break;
				case TextureWrapMode.Repeat:
					sampler.WrapS = WrapMode.Repeat;
					sampler.WrapT = WrapMode.Repeat;
					break;
				case TextureWrapMode.Mirror:
					sampler.WrapS = WrapMode.MirroredRepeat;
					sampler.WrapT = WrapMode.MirroredRepeat;
					break;
				default:
					Debug.LogWarning("Unsupported Texture.wrapMode: " + texture.wrapMode, texture);
					sampler.WrapS = WrapMode.Repeat;
					sampler.WrapT = WrapMode.Repeat;
					break;
			}

			if(dataForTexture.HasMipmaps)
			{
				switch (texture.filterMode)
				{
					case FilterMode.Point:
						sampler.MinFilter = MinFilterMode.NearestMipmapNearest;
						sampler.MagFilter = MagFilterMode.Nearest;
						break;
					case FilterMode.Bilinear:
						// not technically correct but matches result in Unity much better. When any aniso is on, the expectation is a smooth texture transition.
						sampler.MinFilter = dataForTexture.HasAniso ? MinFilterMode.LinearMipmapLinear : MinFilterMode.LinearMipmapNearest;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
					case FilterMode.Trilinear:
						sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
					default:
						Debug.LogWarning("Unsupported Texture.filterMode: " + texture.filterMode, texture);
						sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
				}
			}
			else
			{
				switch (texture.filterMode)
				{
					case FilterMode.Point:
						sampler.MinFilter = MinFilterMode.Nearest;
						sampler.MagFilter = MagFilterMode.Nearest;
						break;
					default:
						sampler.MinFilter = MinFilterMode.Linear;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
				}
			}

			var samplerId = new SamplerId
			{
				Id = _root.Samplers.Count,
				Root = _root
			};

			_root.Samplers.Add(sampler);
			_textureSettingsToSamplerIndices.Add(dataForTexture, samplerId.Id);

			return samplerId;
		}
	}
}
