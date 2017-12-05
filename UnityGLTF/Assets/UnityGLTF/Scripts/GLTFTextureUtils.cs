using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class GLTFTextureUtils
{
	private static string _flipTexture = "GLTF/FlipTexture";
	private static string _packOcclusionMetalRough = "GLTF/PackOcclusionMetalRough";
	private static string _convertBump = "GLTF/BumpToNormal";
	public static bool _useOriginalImages = true;

	public static void setUseOriginalImage(bool useOriginal)
	{
		_useOriginalImages = useOriginal;
	}

	public static void setSRGB(bool useSRGB)
	{
		GL.sRGBWrite = useSRGB;
	}

	public static string writeTextureOnDisk(Texture2D texture, string outputPath, bool updateExtension=false)
	{
		string finalOutputPath = outputPath;
		byte[] finalImageData = Path.GetExtension(finalOutputPath) == ".jpg" ? texture.EncodeToJPG() : texture.EncodeToPNG();

		File.WriteAllBytes(finalOutputPath, finalImageData);
		AssetDatabase.Refresh();

		return finalOutputPath;
	}

	// Export
	public static Texture2D bumpToNormal(Texture2D texture)
	{
		Material convertBump = new Material(Shader.Find(_convertBump));
		convertBump.SetTexture("_BumpMap", texture);
		return processTextureMaterial(texture, convertBump);
	}

	public static bool isNormalMapFromGrayScale(ref Texture2D texture)
	{
		TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
		if (im == null)
			return false;

		return im.convertToNormalmap;
	}

	public static Texture2D packOcclusionMetalRough(Texture2D metallicSmoothnessMap, Texture2D occlusionMap)
	{
		if(metallicSmoothnessMap == null && occlusionMap == null)
		{
			return null;
		}

		bool srgb = GL.sRGBWrite;
		GL.sRGBWrite = false;

		Material packMaterial = new Material(Shader.Find(_packOcclusionMetalRough));
		Texture2D tex = null;
		if(metallicSmoothnessMap)
		{
			tex = metallicSmoothnessMap;
			packMaterial.SetTexture("_MetallicGlossMap", metallicSmoothnessMap);
		}

		if(occlusionMap)
		{
			if(tex == null)
			{
				tex = occlusionMap;
			}

			packMaterial.SetTexture("_OcclusionMap", occlusionMap);
		}
		Texture2D result = processTextureMaterial(tex, packMaterial);
		GL.sRGBWrite = srgb;
		return result;
	}

	// CORE
	private static Texture2D processTextureMaterial(Texture2D texture, Material blitMaterial, bool isRGB=false)
	{
		var exportTexture = new Texture2D(texture.width, texture.height, (isRGB ? TextureFormat.RGB24 : TextureFormat.ARGB32), false);
		exportTexture.name = texture.name;

		var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32, RenderTextureFormat.ARGB32);
		Graphics.Blit(exportTexture, renderTexture, blitMaterial);
		RenderTexture.active = renderTexture;

		exportTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		exportTexture.Apply();

		RenderTexture.ReleaseTemporary(renderTexture);

		return exportTexture;
	}

	// Normal map should be exported with srgb true
	public static Texture2D handleNormalMap(Texture2D input)
	{
		TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(input)) as TextureImporter;
		if(AssetDatabase.GetAssetPath(input).Length == 0 || im == null || im.convertToNormalmap)
		{
			Debug.Log("Convert bump to normal " + input.name);
			return bumpToNormal(input);
		}
		else
		{
			return getTexture(input);
		}
	}

	private static Texture2D getTexture(Texture2D texture)
	{
		Texture2D temp = new Texture2D(4, 4);
		temp.name = texture.name;
		if (_useOriginalImages)
		{
			if (AssetDatabase.GetAssetPath(texture).Length > 0)
			{
				temp.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)));
				temp.name = texture.name;
			}
			else
			{
				temp = texture;
				Debug.Log("Texture asset is not serialized. Cannot use uncompressed version for " + texture.name);
			}
		}
		else
		{
			temp = texture;
		}

		return temp;
	}

	public static Texture2D flipTexture(Texture2D texture)
	{
		Material flipMaterial = new Material(Shader.Find(_flipTexture));
		Texture2D temp = texture;

		flipMaterial.SetTexture("_TextureToFlip", temp);
		return processTextureMaterial(temp, flipMaterial, useJPGTexture(texture));
	}

	public static bool useJPGTexture(Texture2D texture)
	{
		switch(texture.format)
		{
			case TextureFormat.RGB24:
			case TextureFormat.DXT1:
				return true;
			default:
				return false;
		}
	}
}

class GLTFTextureUtilsCache
{
	private Dictionary<KeyValuePair<Texture2D, Texture2D>, Texture2D> _packedTextures;
	private Dictionary<Texture2D, Texture2D> _convertedBump;
	private Dictionary<Texture2D, Texture2D> _flippedTextures;

	public GLTFTextureUtilsCache()
	{
		_packedTextures = new Dictionary<KeyValuePair<Texture2D, Texture2D>, Texture2D>();
		_convertedBump = new Dictionary<Texture2D, Texture2D>();
		_flippedTextures = new Dictionary<Texture2D, Texture2D>();
	}

	public Texture2D packOcclusionMetalRough(Texture2D metalSmooth, Texture2D occlusion)
	{
		KeyValuePair<Texture2D, Texture2D> key = new KeyValuePair<Texture2D, Texture2D>(metalSmooth, occlusion);
		if (!_packedTextures.ContainsKey(key))
		{
			Texture2D tex = GLTFTextureUtils.packOcclusionMetalRough(metalSmooth, occlusion);
			_packedTextures.Add(key, tex);
		}

		return _packedTextures[key];
	}

	public Texture2D handleNormalMap(Texture2D texture)
	{
		if(!_convertedBump.ContainsKey(texture))
		{
			Texture2D tex = GLTFTextureUtils.handleNormalMap(texture);
			_convertedBump.Add(texture, tex);
		}

		return _convertedBump[texture];
	}

	public Texture2D flipTexture(Texture2D texture)
	{
		if(!_flippedTextures.ContainsKey(texture))
		{
			Texture2D flipped = GLTFTextureUtils.flipTexture(texture);
			_flippedTextures.Add(texture, flipped);
		}

		return _flippedTextures[texture];
	}
}