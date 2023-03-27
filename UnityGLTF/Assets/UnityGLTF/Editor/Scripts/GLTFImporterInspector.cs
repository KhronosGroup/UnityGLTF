#if UNITY_2017_1_OR_NEWER
using System;
using System.IO;
using System.Linq;
using System.Text;
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
	internal class GLTFImporterInspector : UnityGLTFTabbedEditor
	{
		private string[] _importNormalsNames;

		public override void OnEnable()
		{
			AddTab(new GltfAssetImporterTab(this, "Model", ModelInspectorGUI));
			AddTab(new GltfAssetImporterTab(this, "Animation", AnimationInspectorGUI));
			AddTab(new GltfAssetImporterTab(this, "Materials", MaterialInspectorGUI));
			AddTab(new GltfAssetImporterTab(this, "Extensions", ExtensionInspectorGUI));

			base.OnEnable();
		}

		private void TextureWarningsGUI(GLTFImporter t)
		{
			if (!t)	return;
			if (!TextureImportSettingsAreCorrect(t))
			{
				EditorGUILayout.HelpBox("Some Textures have incorrect linear/sRGB settings. Results might be incorrect.", MessageType.Warning);
				if (GUILayout.Button("Fix All"))
				{
					FixTextureImportSettings(t);
				}
			}
		}

		private void ModelInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) EditorGUILayout.LabelField("NOOOO");

			if (!t) return;

			// serializedObject.Update();
			if (_importNormalsNames == null)
			{
				_importNormalsNames = Enum.GetNames(typeof(GLTFImporterNormals))
					.Select(n => ObjectNames.NicifyVariableName(n))
					.ToArray();
			}
			EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._removeEmptyRootObjects)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._scaleFactor)));
			// EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._maximumLod)), new GUIContent("Maximum Shader LOD"));
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

			TextureWarningsGUI(t);
		}

		private void AnimationInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) return;

			var anim = serializedObject.FindProperty(nameof(GLTFImporter._importAnimations));
			EditorGUILayout.PropertyField(anim, new GUIContent("Animation Type"));
			if (anim.enumValueIndex > 0)
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

			// warn if Humanoid rig import has failed
			if (!HasModified() && anim.enumValueIndex == (int) AnimationMethod.MecanimHumanoid && !AssetDatabase.LoadAssetAtPath<Avatar>(t.assetPath))
			{
				EditorGUILayout.Separator();
				EditorGUILayout.HelpBox("The model doesn't contain a valid Humanoid rig. See the console for more information.", MessageType.Error);
			}
		}

		private void MaterialInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) return;

			var mats = serializedObject.FindProperty("m_Materials");
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importMaterials)));
			EditorGUILayout.Separator();

			// extract and remap materials
			if (mats != null && mats.serializedObject != null)
			{
				EditorGUI.indentLevel++;
				var externalObjectMap = t.GetExternalObjectMap();

				void ExtractMaterial(Material subAsset, bool importImmediately)
				{
					if (!subAsset) return;
					var filename = SanitizePath(subAsset.name);
					var dirName = Path.GetDirectoryName(t.assetPath) + "/Materials";
					if (!Directory.Exists(dirName))
						Directory.CreateDirectory(dirName);
					var destinationPath = dirName + "/" + filename + ".mat";
					var assetPath = AssetDatabase.GetAssetPath(subAsset);

					var clone = Instantiate(subAsset);
					AssetDatabase.CreateAsset(clone, destinationPath);

					var assetImporter = AssetImporter.GetAtPath(assetPath);
					assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);

					if (importImmediately)
					{
						AssetDatabase.WriteImportSettingsIfDirty(assetPath);
						AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
					}
				}

				const string key2 = nameof(GLTFImporterInspector) + "_RemapMaterials_List";
				var newVal2 = EditorGUILayout.BeginFoldoutHeaderGroup(SessionState.GetBool(key2, false), "Remap Materials (" + mats.arraySize + ")");
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
							if (newObj && newObj != mat)
								t.AddRemap(id, newObj);
							else
								t.RemoveRemap(id);
						}

						if (!remap)
						{
							if (GUILayout.Button("Extract", GUILayout.Width(60)))
							{
								ExtractMaterial(mat, true);
								GUIUtility.ExitGUI();
							}
						}
						else
						{
							if (GUILayout.Button("Restore", GUILayout.Width(60)))
							{
								t.RemoveRemap(id);
#if UNITY_2022_2_OR_NEWER
								SaveChanges();
#else
								ApplyAndImport();
#endif
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
						ExtractMaterial(mats.GetArrayElementAtIndex(i).objectReferenceValue as Material, false);
						AssetDatabase.StopAssetEditing();
						var assetPath = AssetDatabase.GetAssetPath(target);
						AssetDatabase.WriteImportSettingsIfDirty(assetPath);
						AssetDatabase.Refresh();
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

			TextureWarningsGUI(t);
		}

		private void ExtensionInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) return;

			EditorGUI.BeginDisabledGroup(true);
			var mainAssetIdentifierProp = serializedObject.FindProperty(nameof(GLTFImporter._mainAssetIdentifier));
			EditorGUILayout.PropertyField(mainAssetIdentifierProp);

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._extensions)), new GUIContent("Extensions"));
			EditorGUI.EndDisabledGroup();
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
