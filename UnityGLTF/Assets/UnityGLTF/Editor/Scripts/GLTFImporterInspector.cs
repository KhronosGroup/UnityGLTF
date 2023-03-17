#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
	internal class GLTFImporterInspector : UnityGLTFTabbedEditor
	{
		private string[] _importNormalsNames;

		public override void TabInspectorGUI()
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
			EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._removeEmptyRootObjects)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._scaleFactor)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._maximumLod)), new GUIContent("Maximum Shader LOD"));
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._readWriteEnabled)), new GUIContent("Read/Write"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._generateColliders)));
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();
			var importNormalsProp = serializedObject.FindProperty(nameof(GLTFImporter._importNormals));
			var importNormals = EditorGUILayout.Popup("Normals", importNormalsProp.intValue, _importNormalsNames);
			if (EditorGUI.EndChangeCheck())
			{
				importNormalsProp.intValue = importNormals;
			}
			EditorGUI.BeginChangeCheck();
			var importTangentsProp = serializedObject.FindProperty(nameof(GLTFImporter._importTangents));
			var importTangents = EditorGUILayout.Popup("Tangents", importTangentsProp.intValue, _importNormalsNames);
			if (EditorGUI.EndChangeCheck())
			{
				importTangentsProp.intValue = importTangents;
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._swapUvs)), new GUIContent("Swap UVs"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._generateLightmapUVs)), new GUIContent("Generate Lightmap UVs"));
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
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

			EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
			var mats = serializedObject.FindProperty("m_Materials");
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importMaterials)));
			EditorGUILayout.Separator();

			const string key = nameof(GLTFImporterInspector) + "_RemapMaterials";
			var newVal = EditorGUILayout.BeginFoldoutHeaderGroup(SessionState.GetBool(key, false), "Remap Materials");
			SessionState.SetBool(key, newVal);
			// extract and remap materials
			if (newVal && mats != null && mats.serializedObject != null)
			{
				EditorGUI.indentLevel++;
				var externalObjectMap = t.GetExternalObjectMap();

				void ExtractMaterial(Material subAsset)
				{
					if (!subAsset) return;
					var filename = SanitizePath(subAsset.name);
					var destinationPath = Path.GetDirectoryName(t.assetPath) + "/" + filename + ".mat";
					var assetPath = AssetDatabase.GetAssetPath(subAsset);

					var clone = Instantiate(subAsset);
					AssetDatabase.CreateAsset(clone, destinationPath);

					var assetImporter = AssetImporter.GetAtPath(assetPath);
					assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);

					AssetDatabase.WriteImportSettingsIfDirty(assetPath);
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				}

				const string key2 = nameof(GLTFImporterInspector) + "_RemapMaterials_List";
				var newVal2 = EditorGUILayout.Foldout(SessionState.GetBool(key2, false), "All Materials (" + mats.arraySize + ")");
				SessionState.SetBool(key2, newVal2);
				if (newVal2)
				{
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
							{
								ExtractMaterial(mat);
								GUIUtility.ExitGUI();
							}
						}
						else
						{
							if (GUILayout.Button("Restore", GUILayout.Width(60)))
							{
								t.RemoveRemap(id);
								ApplyAndImport();
								GUIUtility.ExitGUI();
							}
						}

						EditorGUILayout.EndHorizontal();
					}
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
						AssetDatabase.StartAssetEditing();
						ExtractMaterial(mats.GetArrayElementAtIndex(i).objectReferenceValue as Material);
						AssetDatabase.StopAssetEditing();
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

			if (!TextureImportSettingsAreCorrect(t))
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
			// ApplyRevertGUI();
		}

		public static bool TextureImportSettingsAreCorrect(GLTFImporter importer)
		{
			return importer.Textures.All(x =>
			{
				if (AssetDatabase.Contains(x.texture) && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(x.texture)) is TextureImporter textureImporter)
				{
					return textureImporter.sRGBTexture == !x.shouldBeLinear;
				}
				return true;
			});
		}

		public static void FixTextureImportSettings(GLTFImporter importer)
		{
			var haveStartedAssetEditing = false;
			foreach (var x in importer.Textures)
			{
				if (!AssetDatabase.Contains(x.texture) || !(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(x.texture)) is TextureImporter textureImporter)) continue;

				// skip if already correct
				if (textureImporter.sRGBTexture == !x.shouldBeLinear)
					continue;

				if (!haveStartedAssetEditing)
				{
					haveStartedAssetEditing = true;
					AssetDatabase.StartAssetEditing();
				}

				textureImporter.sRGBTexture = !x.shouldBeLinear;
				textureImporter.SaveAndReimport();
			}

			if (haveStartedAssetEditing)
				AssetDatabase.StopAssetEditing();
		}

		private static string SanitizePath(string subAssetName)
		{
			// make filename safe without using Regex
			var invalidChars = Path.GetInvalidFileNameChars();
			var sb = new StringBuilder(subAssetName);
			for (int i = 0; i < sb.Length; i++)
			{
				if (invalidChars.Contains(sb[i]))
				{
					sb[i] = '_';
				}
			}
			return sb.ToString();
		}
	}
}
#endif
