#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

using UnityEngine;
using Object = UnityEngine.Object;
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
			var anim = serializedObject.FindProperty(nameof(GLTFImporter._importAnimations));
			EditorGUILayout.PropertyField(anim, new GUIContent("Animations"));
			if (anim.boolValue)
			{
				var loopTime = serializedObject.FindProperty(nameof(GLTFImporter._animationLoopTime));
				EditorGUILayout.PropertyField(loopTime, new GUIContent("Loop Time"));
				if (loopTime.boolValue)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._animationLoopPose)), new GUIContent("Loop Pose"));
					EditorGUI.indentLevel--;
				}
			}
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
			var mats = serializedObject.FindProperty("m_Materials");
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importMaterials)));

			EditorGUILayout.Separator();
			const string key = nameof(GLTFImporterInspector) + "_RemapMaterials";
			var newVal = EditorGUILayout.BeginFoldoutHeaderGroup(SessionState.GetBool(key, false), "Remap Materials");
			SessionState.SetBool(key, newVal);
			// EditorGUILayout.LabelField("Remap Materials", EditorStyles.boldLabel);
			// extract and remap materials
			if (newVal)
			{
				EditorGUI.indentLevel++;
				var externalObjectMap = t.GetExternalObjectMap();

				void ExtractMaterial(Material subAsset)
				{
					if (!subAsset) return;
					var destinationPath = Path.GetDirectoryName(t.assetPath) + "/" + subAsset.name + ".mat";
					string assetPath = AssetDatabase.GetAssetPath(subAsset);

					var clone = Instantiate(subAsset);
					AssetDatabase.CreateAsset(clone, destinationPath);

					var assetImporter = AssetImporter.GetAtPath(assetPath);
					assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);

					AssetDatabase.WriteImportSettingsIfDirty(assetPath);
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				}

				for (var i = 0; i < mats.arraySize; i++)
				{
					var mat = mats.GetArrayElementAtIndex(i).objectReferenceValue as Material;
					if (!mat) continue;
					var id = new AssetImporter.SourceAssetIdentifier(mat);
					externalObjectMap.TryGetValue(id, out var remap);
					EditorGUILayout.BeginHorizontal();
					// EditorGUILayout.ObjectField(/*mat.name,*/ mat, typeof(Material), false);
					EditorGUI.BeginChangeCheck();
					var newObj = EditorGUILayout.ObjectField(mat.name, remap, typeof(Material), false);
					if (EditorGUI.EndChangeCheck())
					{
						if (newObj)
							t.AddRemap(id, newObj);
						else
							t.RemoveRemap(id);
					}

					if (!remap)
					{
						if (GUILayout.Button("Extract", GUILayout.Width(60)))
							ExtractMaterial(mat);
					}
					else
					{
						if (GUILayout.Button("Restore", GUILayout.Width(60))) {
							t.RemoveRemap(id);
							ApplyAndImport();
						}
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				if (GUILayout.Button("Restore Materials"))
				{
					for (var i = 0; i < mats.arraySize; i++)
					{
						var mat = mats.GetArrayElementAtIndex(i).objectReferenceValue as Material;
						if (!mat) continue;
						t.RemoveRemap(new AssetImporter.SourceAssetIdentifier(mat));
					}
				}

				if (GUILayout.Button("Extract Materials"))
				{
					for (var i = 0; i < mats.arraySize; i++)
					{
						ExtractMaterial(mats.GetArrayElementAtIndex(i).objectReferenceValue as Material);
					}
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();

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
