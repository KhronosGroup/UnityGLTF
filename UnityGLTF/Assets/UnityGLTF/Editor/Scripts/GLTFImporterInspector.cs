#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityGLTF
{
	[CustomEditor(typeof(GLTFImporter))]
	[CanEditMultipleObjects]
	public class GLTFImporterInspector : AssetImporterEditor
	{
		private string[] _importNormalsNames;

		public override void OnInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) return;

			serializedObject.Update();
			if (_importNormalsNames == null)
			{
				_importNormalsNames = Enum.GetNames(typeof(GLTFImporterNormals))
					.Select(n => ObjectNames.NicifyVariableName(n))
					.ToArray();
			}
			EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._removeEmptyRootObjects)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._scaleFactor)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._maximumLod)), new GUIContent("Maximum LOD"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._readWriteEnabled)), new GUIContent("Read/Write Enabled"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._generateColliders)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._swapUvs)), new GUIContent("Swap UVs"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._generateLightmapUVs)), new GUIContent("Generate Lightmap UVs"));
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importAnimations)), new GUIContent("Animations"));
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Mesh Data", EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();
			var importNormalsProp = serializedObject.FindProperty(nameof(GLTFImporter._importNormals));
			var importNormals = EditorGUILayout.Popup(importNormalsProp.displayName, importNormalsProp.intValue, _importNormalsNames);
			if (EditorGUI.EndChangeCheck())
			{
				importNormalsProp.intValue = importNormals;
			}
			EditorGUI.BeginChangeCheck();
			var importTangentsProp = serializedObject.FindProperty(nameof(GLTFImporter._importTangents));
			var importTangents = EditorGUILayout.Popup(importTangentsProp.displayName, importTangentsProp.intValue, _importNormalsNames);
			if (EditorGUI.EndChangeCheck())
			{
				importTangentsProp.intValue = importTangents;
			}
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importMaterials)));
			EditorGUILayout.Separator();

			var identifierProp = serializedObject.FindProperty(nameof(GLTFImporter._useSceneNameIdentifier));
			if (!identifierProp.boolValue)
			{
				EditorGUILayout.HelpBox("This asset uses the old main asset identifier. Upgrade to the new one if you want to seamlessly switch between glTFast and UnityGLTF on import.\nNOTE: This will break existing scene references.", MessageType.Info);
				if (GUILayout.Button("Update Asset Identifier"))
				{
					identifierProp.boolValue = true;
					serializedObject.ApplyModifiedProperties();
					try
					{
#if UNITY_2022_2_OR_NEWER
						SaveChanges();
#else
						ApplyAndImport();
#endif
						GUIUtility.ExitGUI();
					}
					catch
					{
						// ignore - seems in some cases the GameObjectInspector will throw
					}
				}
			}
			// EditorGUILayout.PropertyField(serializedObject.FindProperty("_useJpgTextures"), new GUIContent("Use JPG Textures"));

			EditorGUILayout.Separator();
			var texturesHaveCorrectImportSettings = t.Textures.All(x =>
			{
				if (AssetDatabase.Contains(x.texture) && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(x.texture)) is TextureImporter textureImporter)
				{
					return textureImporter.sRGBTexture == !x.shouldBeLinear;
				}
				return true;
			});

			if (!texturesHaveCorrectImportSettings)
			{
				EditorGUILayout.HelpBox("Some Textures have incorrect linear/sRGB settings. Results might be incorrect.", MessageType.Warning);
				if (GUILayout.Button("Fix All"))
				{
					FixTextureImportSettings(t);
				}
			}
			EditorGUI.BeginDisabledGroup(true);
			var mainAssetIdentifierProp = serializedObject.FindProperty(nameof(GLTFImporter._mainAssetIdentifier));
			EditorGUILayout.PropertyField(mainAssetIdentifierProp);
			// EditorGUILayout.PropertyField(serializedObject.FindProperty("_textures"), new GUIContent("Textures"));
			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._extensions)), new GUIContent("Extensions"));
			EditorGUI.EndDisabledGroup();

			serializedObject.ApplyModifiedProperties();
			ApplyRevertGUI();
		}

		public static void FixTextureImportSettings(GLTFImporter importer)
		{
			AssetDatabase.StartAssetEditing();
			foreach (var x in importer.Textures)
			{
				if (!AssetDatabase.Contains(x.texture) || !(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(x.texture)) is TextureImporter textureImporter)) continue;

				textureImporter.sRGBTexture = !x.shouldBeLinear;
				textureImporter.SaveAndReimport();
			}
			AssetDatabase.StopAssetEditing();
		}
	}
}
#endif
