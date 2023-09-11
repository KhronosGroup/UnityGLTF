#if UNITY_2017_1_OR_NEWER
using System;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
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
			if (!this) return;

			var m_HasSceneData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasSceneData));
			if (m_HasSceneData.boolValue)
				AddTab(new GltfAssetImporterTab(this, "Model", ModelInspectorGUI));

			var m_HasAnimationData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasAnimationData));
			if (m_HasAnimationData.boolValue)
				AddTab(new GltfAssetImporterTab(this, "Animation", AnimationInspectorGUI));

			var m_HasMaterialData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasMaterialData));
			var m_HasTextureData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasTextureData));
			if (m_HasMaterialData.boolValue || m_HasTextureData.boolValue)
				AddTab(new GltfAssetImporterTab(this, "Materials", MaterialInspectorGUI));

			AddTab(new GltfAssetImporterTab(this, "Extensions", ExtensionInspectorGUI));

			base.OnEnable();
		}

		private void TextureWarningsGUI(GLTFImporter t)
		{
			if (!t)	return;
			if (!GLTFImporterHelper.TextureImportSettingsAreCorrect(t))
			{
				EditorGUILayout.HelpBox("Some Textures have incorrect linear/sRGB settings. Results might be incorrect.", MessageType.Warning);
				if (GUILayout.Button("Fix All"))
				{
					GLTFImporterHelper.FixTextureImportSettings(t);
				}
			}
		}

		private void ModelInspectorGUI()
		{
			var t = target as GLTFImporter;
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

			// Show texture compression UI if there are any imported textures 
			var importedTextures = serializedObject.FindProperty("m_Textures");
			if (importedTextures.arraySize > 0)
			{
				EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._textureCompression)));
			}
			TextureWarningsGUI(t);
		}

		private const string TextureRemappingKey = nameof(GLTFImporterInspector) + "_TextureRemapping";
		private bool EnableTextureRemapping
		{
			get => SessionState.GetBool(TextureRemappingKey + target.GetInstanceID(), false);
			set => SessionState.SetBool(TextureRemappingKey + target.GetInstanceID(), value);
		}
		private static readonly GUIContent RemapTexturesToggleContent = new GUIContent("Experimental", "(experimental) Remap textures inside the glTF to textures that are already in your project.");

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

			// show animations for clip import editing
			var animations = serializedObject.FindProperty("m_Animations");
			if (animations.arraySize > 0)
			{
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(animations, new GUIContent("Animations"), true);
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

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importMaterials)));
			var importedMaterials = serializedObject.FindProperty("m_Materials");
			if (importedMaterials.arraySize > 0)
			{
				RemappingUI<Material>(t, importedMaterials, "Materials", ".mat");
			}

			// There's a bunch of known edge cases with texture remapping that can go wrong,
			// So it's disabled for now.
			var current = EnableTextureRemapping;
			var val = EditorGUILayout.Toggle(RemapTexturesToggleContent, EnableTextureRemapping);
			if (val != current) EnableTextureRemapping = val;
			var importedTextures = serializedObject.FindProperty("m_Textures");
			if (EnableTextureRemapping && importedTextures.arraySize > 0)
			{
				RemappingUI<Texture>(t, importedTextures, "Textures", ".asset");
			}

			var identifierProp = serializedObject.FindProperty(nameof(GLTFImporter._useSceneNameIdentifier));
			if (!identifierProp.boolValue)
			{
				EditorGUILayout.Space();
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

			EditorGUILayout.Separator();

			TextureWarningsGUI(t);
		}

		private void RemappingUI<T>(GLTFImporter t, SerializedProperty importedData, string subDirectoryName, string fileExtension) where T: UnityEngine.Object
		{
			// extract and remap materials
			if (importedData != null && importedData.serializedObject != null)
			{
				EditorGUI.indentLevel++;

				var externalObjectMap = t.GetExternalObjectMap();
				// TODO this also counts old remaps that are not used anymore
				var remapCount = externalObjectMap.Values.Count(x => x is T);

				void ExtractAsset(T subAsset, bool importImmediately)
				{
					if (!subAsset) return;
					var filename = SanitizePath(subAsset.name);
					var dirName = Path.GetDirectoryName(t.assetPath) + "/" + subDirectoryName;
					if (!Directory.Exists(dirName))
						Directory.CreateDirectory(dirName);
					var destinationPath = dirName + "/" + filename + fileExtension;
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

				string remapFoldoutKey = nameof(GLTFImporterInspector) + "_Remap_" + subDirectoryName + "_Foldout";
				var remapFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(SessionState.GetBool(remapFoldoutKey, false), "Remapped " + subDirectoryName + " (" + remapCount + " / " + importedData.arraySize + ")");
				SessionState.SetBool(remapFoldoutKey, remapFoldout);
				if (remapFoldout)
				{
					if (remapCount > 0)
					{
						EditorGUILayout.BeginHorizontal();

						if (GUILayout.Button("Restore all " + subDirectoryName))
						{
							for (var i = 0; i < importedData.arraySize; i++)
							{
								var mat = importedData.GetArrayElementAtIndex(i).objectReferenceValue as T;
								if (!mat) continue;
								t.RemoveRemap(new AssetImporter.SourceAssetIdentifier(mat));
							}

							// also remove all old remaps
							var oldRemaps = externalObjectMap.Where(x => x.Value is T).ToList();
							foreach (var oldRemap in oldRemaps)
							{
								t.RemoveRemap(oldRemap.Key);
							}
						}

						if (typeof(T) == typeof(Material) && GUILayout.Button("Extract all " + subDirectoryName))
						{
							var materials = new T[importedData.arraySize];
							for (var i = 0; i < importedData.arraySize; i++)
								materials[i] = importedData.GetArrayElementAtIndex(i).objectReferenceValue as T;

							for (var i = 0; i < materials.Length; i++)
							{
								if (!materials[i]) continue;
								AssetDatabase.StartAssetEditing();
								ExtractAsset(materials[i], false);
								AssetDatabase.StopAssetEditing();
								var assetPath = AssetDatabase.GetAssetPath(target);
								AssetDatabase.WriteImportSettingsIfDirty(assetPath);
								AssetDatabase.Refresh();
							}
						}

						EditorGUILayout.EndHorizontal();
					}

					for (var i = 0; i < importedData.arraySize; i++)
					{
						var mat = importedData.GetArrayElementAtIndex(i).objectReferenceValue as T;
						if (!mat) continue;
						var id = new AssetImporter.SourceAssetIdentifier(mat);
						externalObjectMap.TryGetValue(id, out var remap);
						EditorGUILayout.BeginHorizontal();
						// EditorGUILayout.ObjectField(/*mat.name,*/ mat, typeof(Material), false);
						EditorGUI.BeginChangeCheck();
						var newObj = EditorGUILayout.ObjectField(mat.name, remap, typeof(T), false);
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
								ExtractAsset(mat, true);
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

					EditorGUILayout.Space();
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
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

			// TODO add list of supported extensions and links to docs
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

	public static class GLTFImporterHelper
	{
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
	}
}
#endif
