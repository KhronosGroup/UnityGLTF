using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityGLTF.Cache;

public class AssetManager
{
	// Files/Directories
	protected string _importDirectory;
	protected string _importMeshesDirectory;
	protected string _importMaterialsDirectory;
	protected string _importTexturesDirectory;
	public List<string> _generatedFiles;

	// Import data
	public List<GameObject> _createdGameObjects;
	public List<List<KeyValuePair<Mesh, Material>>> _parsedMeshData;
	public List<Material> _parsedMaterials;
	public List<Texture2D> _parsedImages;
	public List<Texture2D> _parsedTextures;
	public List<int> _usedSources;
	private Object _prefab;

	public AssetManager(string projectDirectoryPath)
	{
		// Prepare hierarchy un project
		_importDirectory = projectDirectoryPath;
		Directory.CreateDirectory(_importDirectory);

		_importTexturesDirectory = Path.Combine(_importDirectory, "textures");
		Directory.CreateDirectory(_importTexturesDirectory);

		_importMeshesDirectory = Path.Combine(_importDirectory, "meshes");
		Directory.CreateDirectory(_importMeshesDirectory);

		_importMaterialsDirectory = Path.Combine(_importDirectory, "materials");
		Directory.CreateDirectory(_importMaterialsDirectory);

		_createdGameObjects = new List<GameObject>();
		_parsedMeshData = new List<List<KeyValuePair<Mesh, Material>>>();
		_parsedMaterials = new List<Material>();
		_parsedImages = new List<Texture2D>();
		_parsedTextures = new List<Texture2D>();
		_usedSources = new List<int>();
		_generatedFiles = new List<string>();
	}

	public void softClean()
	{
		_parsedMeshData.Clear();
		_parsedImages.Clear();
		_parsedTextures.Clear();
		_parsedMaterials.Clear();
		_usedSources.Clear();

		for (int i = 0; i < _createdGameObjects.Count; ++i)
		{
			Object.DestroyImmediate(_createdGameObjects[i]);
		}
		_createdGameObjects.Clear();
		AssetDatabase.Refresh(); // also triggers Resources.UnloadUnusedAssets()
	}

	public void addModelToScene()
	{
		PrefabUtility.InstantiatePrefab(_prefab);
	}

	public void hardClean()
	{
		softClean();

		for(int i=0; i < _createdGameObjects.Count; ++i)
		{
			Object.DestroyImmediate(_createdGameObjects[i]);
		}

		GLTFUtils.removeFileList(_generatedFiles.ToArray());
		AssetDatabase.Refresh(); // also triggers Resources.UnloadUnusedAssets()

		// Remove directories if empty
		GLTFUtils.removeEmptyDirectory(_importMeshesDirectory);
		GLTFUtils.removeEmptyDirectory(_importTexturesDirectory);
		GLTFUtils.removeEmptyDirectory(_importMaterialsDirectory);
		_createdGameObjects.Clear();

		AssetDatabase.Refresh(); // also triggers Resources.UnloadUnusedAssets()
		GLTFUtils.removeEmptyDirectory(_importDirectory);
		AssetDatabase.Refresh(); // also triggers Resources.UnloadUnusedAssets()
	}

	public GameObject createGameObject(string name)
	{
		GameObject go = new GameObject(name);
		_createdGameObjects.Add(go);
		return go;
	}

	public void addPrimitiveMeshData(int meshIndex, int primitiveIndex, UnityEngine.Mesh mesh, UnityEngine.Material material)
	{
		if(meshIndex >= _parsedMeshData.Count)
		{
			_parsedMeshData.Add(new List<KeyValuePair<Mesh, Material>>());
		}

		if(primitiveIndex != _parsedMeshData[meshIndex].Count)
		{
			Debug.LogError("Array offset in mesh data");
		}

		_parsedMeshData[meshIndex].Add(new KeyValuePair<Mesh, Material>(mesh, material));
	}

	public Mesh getMesh(int nodeIndex, int primitiveIndex)
	{
		return _parsedMeshData[nodeIndex][primitiveIndex].Key;
	}

	public Material getMaterial(int nodeIndex, int primitiveIndex)
	{
		return _parsedMeshData[nodeIndex][primitiveIndex].Value;
	}

	public UnityEngine.Material getMaterial(int index)
	{
		return _parsedMaterials[index];
	}

	public string getImportTextureDir()
	{
		return _importTexturesDirectory;
	}

	public UnityEngine.Texture2D getTexture(int index)
	{
		return _parsedTextures[index];
	}

	public void setTextureNormalMap(int index)
	{
		Texture2D texture = _parsedTextures[index];
		TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
		im.textureType = TextureImporterType.NormalMap;
		im.SaveAndReimport();
	}

	public void updateTexture(Texture2D texture, int imageIndex, int textureIndex)
	{
		_parsedTextures[imageIndex] = texture;
	}

	public Texture2D getOrCreateTexture(int imageIndex, int textureIndex)
	{
		if(_usedSources.Contains(imageIndex))
		{
			// Duplicate image
			string origin = AssetDatabase.GetAssetPath(_parsedImages[imageIndex]);
			string dest = Path.Combine(Path.GetDirectoryName(origin), Path.GetFileNameWithoutExtension(origin) + "_" + textureIndex + Path.GetExtension(origin));
			AssetDatabase.CopyAsset(origin, dest);
			Texture2D duplicate = AssetDatabase.LoadAssetAtPath<Texture2D>(dest);
			return duplicate;
		}
		else
		{
			_usedSources.Add(imageIndex);
			return _parsedImages[imageIndex];
		}
	}

	public void registerTexture(Texture2D texture)
	{
		_parsedTextures.Add(texture);
	}

	public string generateName(string name, int index)
	{
		return GLTFUtils.cleanNonAlphanumeric(name + "_" + index);
	}

	public void registerImageFromData(byte[] imageData, int imageID, string imageName="")
	{
		Texture2D texture = new Texture2D(4, 4);
		texture.name = imageName;
		texture.LoadImage(imageData);
		GL.sRGBWrite = true;
		saveTexture(GLTFTextureUtils.flipTexture(texture), imageID);
	}

	public void copyAndRegisterImageInProject(string inputImage, int imageID)
	{
		string imageName = Path.GetFileNameWithoutExtension(inputImage);
		byte[] imageData = File.ReadAllBytes(inputImage);
		bool srgb = GL.sRGBWrite;
		GL.sRGBWrite = true;
		registerImageFromData(imageData, imageID, imageName);
		GL.sRGBWrite = srgb;
	}

	// File serialization
	public Mesh saveMesh(Mesh mesh, string objectName = "Scene")
	{
		string baseName = GLTFUtils.cleanNonAlphanumeric(objectName + ".asset");
		string fullPath = Path.Combine(_importMeshesDirectory, baseName);
		string meshProjectPath = GLTFUtils.getPathProjectFromAbsolute(fullPath);

		if (!File.Exists(fullPath))
		{
			AssetDatabase.CreateAsset(mesh, meshProjectPath);
			_generatedFiles.Add(fullPath);
			AssetDatabase.Refresh();
		}

		return AssetDatabase.LoadAssetAtPath(meshProjectPath, typeof(Mesh)) as Mesh;
	}

	public Texture2D saveTexture(Texture2D texture, int index = -1, string imageName = "")
	{
		string basename = GLTFUtils.cleanNonAlphanumeric(texture.name + (index >= 0 ? "_" + index.ToString() : "") + ".png"); // Extension will be overridden
		string fullPath = Path.Combine(_importTexturesDirectory, basename);

		// Write texture
		string newAssetPath = GLTFTextureUtils.writeTextureOnDisk(texture, fullPath, true);

		// Reload as asset
		string projectPath = GLTFUtils.getPathProjectFromAbsolute(newAssetPath);
		Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(projectPath, typeof(Texture2D));
		_parsedImages.Add(tex);
		return tex;
	}

	public Material saveMaterial(Material material, int index)
	{
		string baseName = generateName(material.name.Length > 0 ? material.name : "material", index) + ".mat";
		string materialAssetPath = Path.Combine(_importMaterialsDirectory, baseName);
		string materialProjectPath = GLTFUtils.getPathProjectFromAbsolute(materialAssetPath);

		// Write material asset
		if (!File.Exists(materialAssetPath))
		{
			AssetDatabase.CreateAsset(material, materialProjectPath);
			_generatedFiles.Add(materialAssetPath);
			AssetDatabase.Refresh();
		}

		// Reload as asset
		return (Material)AssetDatabase.LoadAssetAtPath(materialProjectPath, typeof(Material));
	}

	public void savePrefab(GameObject sceneObject, string _importDirectory)
	{
		string baseName = GLTFUtils.cleanNonAlphanumeric(sceneObject.name.Length > 0 ? sceneObject.name : "GlTF") + ".prefab";
		string fullPath = Path.Combine(_importDirectory, baseName);
		string prefabPathInProject = GLTFUtils.getPathProjectFromAbsolute(fullPath);
		if (!File.Exists(fullPath))
		{
			Object prefab = PrefabUtility.CreateEmptyPrefab(prefabPathInProject);
			_generatedFiles.Add(fullPath);
			PrefabUtility.ReplacePrefab(sceneObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
			_prefab = prefab;
		}
	}
}

