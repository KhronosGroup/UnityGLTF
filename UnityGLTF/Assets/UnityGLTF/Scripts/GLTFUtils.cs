using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using Ionic.Zip;
using System;
using UnityGLTF.Extensions;
using System.Reflection;
using CurveExtended;

public class GLTFUtils
{
	public enum WorkflowMode
	{
		Specular,
		Metallic,
		Dielectric
	}

	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}

	public enum SmoothnessMapChannel
	{
		SpecularMetallicAlpha,
		AlbedoAlpha,
	}

	public static Transform[] getSceneTransforms()
	{
		var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
		var gameObjects = scene.GetRootGameObjects();
		return Array.ConvertAll(gameObjects, gameObject => gameObject.transform);
	}

	public static Transform[] getSelectedTransforms()
	{
		if (Selection.transforms.Length <= 0)
			throw new Exception("No objects selected, cannot export.");

		return Selection.transforms;
	}

	public static string UnityToSystemPath(string path)
	{
		char unitySeparator = '/';
		char pathSeparator = Path.DirectorySeparatorChar;
		path = path.Replace("Assets", Application.dataPath).Replace(unitySeparator, pathSeparator);
		return path;
	}

	public static string SystemToUnityPath(string path)
	{
		char unitySeparator = '/';
		char pathSeparator = Path.DirectorySeparatorChar;
		path = path.Replace(pathSeparator, unitySeparator).Replace(Application.dataPath, "Assets");
		return path;
	}

	public static string unifyPathSeparator(string path)
	{
		return path.Replace("\\\\", "/").Replace("\\", "/");
	}
	public static string getPathProjectFromAbsolute(string absolutePath)
	{
		return unifyPathSeparator(absolutePath.Replace(Application.dataPath, "Assets"));
	}

	public static string getPathAbsoluteFromProject(string projectPath)
	{
		return unifyPathSeparator(projectPath.Replace("Assets", Application.dataPath));
	}

	public static bool isFolderInProjectDirectory(string path)
	{
		return path.Contains(Application.dataPath);
	}

	public static Regex rgx = new Regex("[^a-zA-Z0-9 -_.]");

	static public string cleanName(string s)
	{
		return rgx.Replace(s, "").Replace("/", " ").Replace("\\", " ").Replace(":", "_").Replace("\"", "");
	}

	static public bool isValidMeshObject(GameObject gameObject)
	{
		return gameObject.GetComponent<MeshFilter>() != null && gameObject.GetComponent<MeshFilter>().sharedMesh != null ||
			   gameObject.GetComponent<SkinnedMeshRenderer>() != null && gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh != null;
	}

	public static void removeEmptyDirectory(string directoryPath)
	{
		if (!Directory.Exists(directoryPath))
			return;

		DirectoryInfo info = new DirectoryInfo(directoryPath);
		if (info.GetFiles().Length == 0)
			Directory.Delete(directoryPath, true);
	}

	public static void removeFileList(string[] fileList)
	{
		foreach (string file in fileList)
		{
			if (File.Exists(file))
				File.Delete(file);
		}

		foreach (string dir in fileList)
		{
			if (Directory.Exists(dir))
				Directory.Delete(dir);
		}
	}

	public static Matrix4x4 convertMatrixLeftToRightHandedness(Matrix4x4 mat)
	{
		Vector3 position = mat.GetColumn(3);
		convertVector3LeftToRightHandedness(ref position);
		Quaternion rotation = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
		convertQuatLeftToRightHandedness(ref rotation);

		Vector3 scale = new Vector3(mat.GetColumn(0).magnitude, mat.GetColumn(1).magnitude, mat.GetColumn(2).magnitude);
		float epsilon = 0.00001f;

		// Some issues can occurs with non uniform scales
		if (Mathf.Abs(scale.x - scale.y) > epsilon || Mathf.Abs(scale.y - scale.z) > epsilon || Mathf.Abs(scale.x - scale.z) > epsilon)
		{
			Debug.LogWarning("A matrix with non uniform scale is being converted from left to right handed system. This code is not working correctly in this case");
		}

		// Handle negative scale component in matrix decomposition
		if (Matrix4x4.Determinant(mat) < 0)
		{
			Quaternion rot = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
			Matrix4x4 corr = Matrix4x4.TRS(mat.GetColumn(3), rot, Vector3.one).inverse;
			Matrix4x4 extractedScale = corr * mat;
			scale = new Vector3(extractedScale.m00, extractedScale.m11, extractedScale.m22);
		}

		// convert transform values from left handed to right handed
		mat.SetTRS(position, rotation, scale);
		Debug.Log("INVERSIOON");
		return mat;
	}

	public static void convertVector3LeftToRightHandedness(ref Vector3 vect)
	{
		vect.z = -vect.z;
	}

	public static void convertVector4LeftToRightHandedness(ref Vector4 vect)
	{
		vect.z = -vect.z;
		vect.w = -vect.w;
	}

	public static void convertQuatLeftToRightHandedness(ref Quaternion quat)
	{
		quat.w = -quat.w;
		quat.z = -quat.z;
	}

	/// Specifies the path and filename for the GLTF Json and binary
	/// </summary>
	/// <param name="filesToZip">Dictionnary where keys are original absolute file paths, and value is directory in zip</param>
	/// <param name="zipPath">Path of the output zip archive</param>
	/// <param name="deleteOriginals">Remove original files after building the zip</param>
	public static void buildZip(Dictionary<string, string> filesToZip, string zipPath, bool deleteOriginals)
	{
		if(filesToZip.Count == 0)
		{
			Debug.LogError("GLTFUtils: no files to zip");
		}

		ZipFile zip = new ZipFile();
		foreach (string originFilePath in filesToZip.Keys)
		{
			if(!File.Exists(originFilePath))
			{
				Debug.LogError("GLTFUtils.buildZip: File " + originFilePath +" not found.");
			}

			zip.AddFile(originFilePath, filesToZip[originFilePath]);
		}
		try
		{
			zip.Save(zipPath);
		}
		catch(IOException e)
		{
			Debug.LogError("Failed to save zip file." + e);
		}

		// Remove all files
		if(deleteOriginals)
		{
			foreach (string pa in filesToZip.Keys)
			{
				if (System.IO.File.Exists(pa))
					System.IO.File.Delete(pa);
			}
		}
	}

	public static string buildImageName(Texture2D image)
	{
		string extension = GLTFTextureUtils.useJPGTexture(image) ? ".jpg": ".png";
		return image.GetInstanceID().ToString().Replace("-", "") + "_" + image.name + extension;
	}

	public static bool getPixelsFromTexture(ref Texture2D texture, out Color[] pixels)
	{
		//Make texture readable
		TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
		if (!im)
		{
			pixels = new Color[1];
			return false;
		}

		bool readable = im.isReadable;
		TextureImporterCompression format = im.textureCompression;
		TextureImporterType type = im.textureType;
		bool isConvertedBump = im.convertToNormalmap;
		bool srgb = im.sRGBTexture;
		im.sRGBTexture = false;

		if (!readable)
			im.isReadable = true;
		if (type != TextureImporterType.Default)
			im.textureType = TextureImporterType.Default;

		im.textureCompression = TextureImporterCompression.Uncompressed;
		im.SaveAndReimport();

		pixels = texture.GetPixels();

		if (!readable)
			im.isReadable = false;
		if (type != TextureImporterType.Default)
			im.textureType = type;

		if (isConvertedBump)
			im.convertToNormalmap = true;

		im.sRGBTexture = srgb;
		im.textureCompression = format;
		im.SaveAndReimport();

		return true;
	}
	public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Opaque:
				material.SetOverrideTag("RenderType", "");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;
			case BlendMode.Cutout:
				material.SetOverrideTag("RenderType", "TransparentCutout");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
				break;
			case BlendMode.Fade:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				break;
			case BlendMode.Transparent:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				break;
		}
	}

	public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
	{
		int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
		if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
			return SmoothnessMapChannel.AlbedoAlpha;
		else
			return SmoothnessMapChannel.SpecularMetallicAlpha;
	}

	public static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
	{
		// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
		// (MaterialProperty value might come from renderer material property block)
		SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
		if (workflowMode == WorkflowMode.Specular)
			SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
		else if (workflowMode == WorkflowMode.Metallic)
			SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
		SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
		SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

		// A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
		// or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
		// The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
		MaterialEditor.FixupEmissiveFlag(material);
		//bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
		SetKeyword(material, "_EMISSION", material.GetTexture("_EmissionMap"));

		if (material.HasProperty("_SmoothnessTextureChannel"))
		{
			SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
		}
	}

	public static void MaterialChanged(Material material, WorkflowMode workflowMode)
	{
		SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

		SetMaterialKeywords(material, workflowMode);
	}

	public static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword(keyword);
		else
			m.DisableKeyword(keyword);
	}

	public static AnimationCurve[] createCurvesFromArrays(float[] times, Vector3[] keyframes, bool isStepInterpolation = false, bool switchHandedness=false)
	{
		AnimationCurve[] curves = new AnimationCurve[3];
		curves[0] = new AnimationCurve();
		curves[1] = new AnimationCurve();
		curves[2] = new AnimationCurve();

		int nbKeys = Mathf.Min(times.Length, keyframes.Length);
		for (int i = 0; i < nbKeys; ++i)
		{
			Vector3 value = keyframes[i];

			if (switchHandedness)
				value = value.switchHandedness();

			curves[0].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.x, CurveExtension.TangentMode.Linear));
			curves[1].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.y, CurveExtension.TangentMode.Linear));
			curves[2].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.z, CurveExtension.TangentMode.Linear));
		}

		return curves;
	}

	public static AnimationCurve[] createCurvesFromArrays(float[] times, Vector4[] keyframes, bool isStepInterpolation = false)
	{
		AnimationCurve[] curves = new AnimationCurve[4];
		curves[0] = new AnimationCurve();
		curves[1] = new AnimationCurve();
		curves[2] = new AnimationCurve();
		curves[3] = new AnimationCurve();

		int nbKeys = Mathf.Min(times.Length, keyframes.Length);
		for (int i = 0; i < nbKeys; ++i)
		{
			Vector4 value = keyframes[i].switchHandedness();
			curves[0].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.x, CurveExtension.TangentMode.Linear));
			curves[1].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.y, CurveExtension.TangentMode.Linear));
			curves[2].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.z, CurveExtension.TangentMode.Linear));
			curves[3].AddKey(CurveExtension.KeyframeUtil.GetNew(times[i], value.w, CurveExtension.TangentMode.Linear));
		}

		return curves;
	}

	public static string buildBlendShapeName(int meshIndex, int targetIndex)
	{
		return "Target_" + meshIndex + "_" + targetIndex;
	}

	public static AnimationCurve[] buildMorphAnimationCurves(float[] times, float[] values, int nbTargets, bool isStepInterpolation=false)
	{
		AnimationCurve[] curves = new AnimationCurve[nbTargets];
		for (int i = 0; i < nbTargets; ++i)
		{
			curves[i] = new AnimationCurve();
		}
		for (int t=0; t < times.Length; ++t)
		{
			for(int i =0; i< nbTargets; ++i)
			{
				curves[i].AddKey(CurveExtension.KeyframeUtil.GetNew(times[t], values[t * nbTargets + i], CurveExtension.TangentMode.Linear));
			}
		}

		return curves;
	}

	public static void addMorphAnimationCurvesToClip(AnimationCurve[] curves, string targetPath, string[] morphTargetNames, ref AnimationClip clip)
	{
		// We expect all curves to have the same length here (glTF provides a weight for all targets at each time)
		if(curves[0].keys.Length > 0)
		{
			for (int c=0; c < curves.Length; ++c)
			{
				CurveExtension.UpdateAllLinearTangents(curves[c]);
				clip.SetCurve(targetPath, typeof(SkinnedMeshRenderer), "blendShape."+	morphTargetNames[c], curves[c]);
			}
		}
	}

	public static void addTranslationCurvesToClip(AnimationCurve[] translationCurves, string targetPath, ref AnimationClip clip)
	{
		if (translationCurves[0].keys.Length > 1){
			CurveExtension.UpdateAllLinearTangents(translationCurves[0]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalPosition.x", translationCurves[0]);
		}

		if (translationCurves[1].keys.Length > 1){
			CurveExtension.UpdateAllLinearTangents(translationCurves[1]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalPosition.y", translationCurves[1]);
		}

		if (translationCurves[2].keys.Length > 1){
			CurveExtension.UpdateAllLinearTangents(translationCurves[2]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalPosition.z", translationCurves[2]);
		}
	}

	public static float framerate = 10;
	public static void addRotationCurvesToClip(AnimationCurve[] rotationCurves, string targetPath, ref AnimationClip clip)
	{
		// Quaternions keyframes: all curves are expected to have the same number of keys.
		if (rotationCurves[0].keys.Length > 1){

			//FIXME?
			// Rotation curves need to be resampled to avoid having weird tangents/interpolation
			// in unity. The issue is not clear yet, but curves go crazy between two keyframe (with same or very
			// close values) when the time difference is more than a certain threshold.
			// It appears to be a Quaternion->Euler->Quaternion conversion related issue (according to forum posts)
			// The only solution for now is to sample curves with a rate that prevent tangents for going crazy.
			CurveExtension.resampleRotationCurves(rotationCurves, framerate);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalRotation.x", rotationCurves[0]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalRotation.y", rotationCurves[1]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalRotation.z", rotationCurves[2]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalRotation.w", rotationCurves[3]);
		}
	}

	public static void addScaleCurvesToClip(AnimationCurve[] scaleCurves, string targetPath, ref AnimationClip clip)
	{
		if (scaleCurves[0].keys.Length > 1){
			CurveExtension.UpdateAllLinearTangents(scaleCurves[0]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalScale.x", scaleCurves[0]);
		}

		if (scaleCurves[1].keys.Length > 1){
			CurveExtension.UpdateAllLinearTangents(scaleCurves[1]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalScale.y", scaleCurves[1]);
		}

		if (scaleCurves[2].keys.Length > 1){
			CurveExtension.UpdateAllLinearTangents(scaleCurves[2]);
			clip.SetCurve(targetPath, typeof(Transform), "m_LocalScale.z", scaleCurves[2]);
		}
	}

	public static float[] Vector4ToArray(Vector4 vector)
	{
		float[] arr = new float[4];
		arr[0] = vector.x;
		arr[1] = vector.y;
		arr[2] = vector.z;
		arr[3] = vector.w;

		return arr;
	}

	public static float[] normalizeBoneWeights(Vector4 weights)
	{
		float sum = weights.x + weights.y + weights.z + weights.w;
		if (sum != 1.0f)
		{
			weights = weights / sum;
		}

		return Vector4ToArray(weights);
	}
}

namespace CurveExtended
{
	public static class CurveExtension
	{
		public static void resampleRotationCurves(AnimationCurve[] curves, float framerate)
		{
			if (curves.Length != 4)
			{
				Debug.Log("Invalid rotation curves. Aborting");
				return;
			}

			// Here we assume all curves have the same number of keyframes, at the same times (since they
			// are quaternion keyframes)
			float start = curves[0].keys[0].time;
			float end = curves[0].keys[curves[0].keys.Length - 1].time;
			float deltatime = 1.0f / framerate;

			for(float t = start; t < end; t+=deltatime)
			{
				curves[0].AddKey(new Keyframe(t, curves[0].Evaluate(t)));
				curves[1].AddKey(new Keyframe(t, curves[1].Evaluate(t)));
				curves[2].AddKey(new Keyframe(t, curves[2].Evaluate(t)));
				curves[3].AddKey(new Keyframe(t, curves[3].Evaluate(t)));
			}
		}
		public static void UpdateAllLinearTangents(this AnimationCurve curve)
		{
			for (int i = 0; i < curve.keys.Length; i++)
			{
				UpdateTangentsFromMode(curve, i);
			}
		}

		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static void UpdateTangentsFromMode(AnimationCurve curve, int index)
		{
			if (index < 0 || index >= curve.length)
				return;
			Keyframe key = curve[index];
			if (KeyframeUtil.GetKeyTangentMode(key, 0) == TangentMode.Linear && index >= 1)
			{
				key.inTangent = CalculateLinearTangent(curve, index, index - 1);
				curve.MoveKey(index, key);
			}
			if (KeyframeUtil.GetKeyTangentMode(key, 1) == TangentMode.Linear && index + 1 < curve.length)
			{
				key.outTangent = CalculateLinearTangent(curve, index, index + 1);
				curve.MoveKey(index, key);
			}
			if (KeyframeUtil.GetKeyTangentMode(key, 0) != TangentMode.Smooth && KeyframeUtil.GetKeyTangentMode(key, 1) != TangentMode.Smooth)
				return;
			curve.SmoothTangents(index, 0.0f);
		}

		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		private static float CalculateLinearTangent(AnimationCurve curve, int index, int toIndex)
		{
			return (float)(((double)curve[index].value - (double)curve[toIndex].value) / ((double)curve[index].time - (double)curve[toIndex].time));
		}

		public enum TangentMode
		{
			Editable = 0,
			Smooth = 1,
			Linear = 2,
			Stepped = Linear | Smooth,
		}

		public enum TangentDirection
		{
			Left,
			Right
		}


		public class KeyframeUtil
		{

			public static Keyframe GetNew(float time, float value, TangentMode leftAndRight)
			{
				return GetNew(time, value, leftAndRight, leftAndRight);
			}

			public static Keyframe GetNew(float time, float value, TangentMode left, TangentMode right)
			{
				object boxed = new Keyframe(time, value); // cant use struct in reflection

				SetKeyBroken(boxed, true);
				SetKeyTangentMode(boxed, 0, left);
				SetKeyTangentMode(boxed, 1, right);

				Keyframe keyframe = (Keyframe)boxed;
				if (left == TangentMode.Stepped)
					keyframe.inTangent = float.PositiveInfinity;
				if (right == TangentMode.Stepped)
					keyframe.outTangent = float.PositiveInfinity;

				return keyframe;
			}


			// UnityEditor.CurveUtility.cs (c) Unity Technologies
			public static void SetKeyTangentMode(object keyframe, int leftRight, TangentMode mode)
			{

				Type t = typeof(UnityEngine.Keyframe);
				FieldInfo field = t.GetField("m_TangentMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				int tangentMode = (int)field.GetValue(keyframe);

				if (leftRight == 0)
				{
					tangentMode &= -7;
					tangentMode |= (int)mode << 1;
				}
				else
				{
					tangentMode &= -25;
					tangentMode |= (int)mode << 3;
				}

				field.SetValue(keyframe, tangentMode);
				if (GetKeyTangentMode(tangentMode, leftRight) == mode)
					return;
				Debug.Log("bug");
			}

			// UnityEditor.CurveUtility.cs (c) Unity Technologies
			public static TangentMode GetKeyTangentMode(int tangentMode, int leftRight)
			{
				if (leftRight == 0)
					return (TangentMode)((tangentMode & 6) >> 1);
				else
					return (TangentMode)((tangentMode & 24) >> 3);
			}

			// UnityEditor.CurveUtility.cs (c) Unity Technologies
			public static TangentMode GetKeyTangentMode(Keyframe keyframe, int leftRight)
			{
				Type t = typeof(UnityEngine.Keyframe);
				FieldInfo field = t.GetField("m_TangentMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				int tangentMode = (int)field.GetValue(keyframe);
				if (leftRight == 0)
					return (TangentMode)((tangentMode & 6) >> 1);
				else
					return (TangentMode)((tangentMode & 24) >> 3);
			}


			// UnityEditor.CurveUtility.cs (c) Unity Technologies
			public static void SetKeyBroken(object keyframe, bool broken)
			{
				Type t = typeof(UnityEngine.Keyframe);
				FieldInfo field = t.GetField("m_TangentMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				int tangentMode = (int)field.GetValue(keyframe);

				if (broken)
					tangentMode |= 1;
				else
					tangentMode &= -2;
				field.SetValue(keyframe, tangentMode);
			}

		}
	}
}