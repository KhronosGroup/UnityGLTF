#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;

using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
using UnityGLTF.Plugins;

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
				AddTab(new GLTFAssetImporterTab(this, "Model", ModelInspectorGUI));

			AddTab(new GLTFAssetImporterTab(this, "Animation", AnimationInspectorGUI));

			var m_HasMaterialData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasMaterialData));
			var m_HasTextureData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasTextureData));
			if (m_HasMaterialData.boolValue || m_HasTextureData.boolValue)
				AddTab(new GLTFAssetImporterTab(this, "Materials", MaterialInspectorGUI));

			AddTab(new GLTFAssetImporterTab(this, "Used Extensions", ExtensionInspectorGUI));

			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			var t = target as GLTFImporter;
			TextureWarningsGUI(t);
			EditorGUILayout.Space();
			base.OnInspectorGUI();
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
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importCamera)));
			// EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._maximumLod)), new GUIContent("Maximum Shader LOD"));
			EditorGUILayout.Separator();
			
			EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._readWriteEnabled)), new GUIContent("Read/Write"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._generateColliders)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._importBlendShapeNames)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._blendShapeFrameWeight)));
			
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

			var hasAnimationData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasAnimationData)).boolValue;

			if (!hasAnimationData)
			{
				EditorGUILayout.HelpBox("File doesn't contain animation data.", MessageType.None);
			}
			
			var anim = serializedObject.FindProperty(nameof(GLTFImporter._importAnimations));
			EditorGUILayout.PropertyField(anim, new GUIContent("Animation Type"));
			if (hasAnimationData && anim.enumValueIndex > 0)
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

			var importMaterialsProp = serializedObject.FindProperty(nameof(GLTFImporter._importMaterials));
			EditorGUILayout.PropertyField(importMaterialsProp);
			if (importMaterialsProp.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._enableGpuInstancing)), new GUIContent("GPU Instancing"));
				EditorGUI.indentLevel--;
			}
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
			// Gather list of all plugins
			var registeredPlugins = GLTFSettings.GetDefaultSettings().ImportPlugins;
			var overridePlugins = t._importPlugins;

			EditorGUILayout.LabelField("OVERRIDE", EditorStyles.miniLabel, GUILayout.Width(60));
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("", GUILayout.Width(16));
			EditorGUILayout.LabelField("ENABLED", EditorStyles.miniLabel, GUILayout.Width(60));
			EditorGUILayout.EndHorizontal();
			
			foreach (var plugin in registeredPlugins)
			{
				if (plugin.AlwaysEnabled) continue; // no need to show
				var pluginType = plugin.GetType().FullName;
				// draw override toggle
				EditorGUILayout.BeginHorizontal();
				var hasWarning = plugin.Warning != null;
				EditorGUI.BeginDisabledGroup(hasWarning);
				var overridePlugin = overridePlugins.FirstOrDefault(x => x.typeName == pluginType);
				var hasOverride = overridePlugin?.overrideEnabled ?? false;
				var hasOverride2 = EditorGUILayout.ToggleLeft("", hasOverride, GUILayout.Width(16));
				if (hasOverride2 != hasOverride)
				{
					hasOverride = hasOverride2;
					// add or remove a ScriptableObject with the plugin
					if (!hasOverride)
					{
						if (overridePlugin != null) overridePlugin.overrideEnabled = false;
					}
					else
					{
						if (overridePlugin != null)
						{
							overridePlugin.overrideEnabled = true;
						}
						else
						{
							var newPlugin = new GLTFImporter.PluginInfo()
							{
								typeName = plugin.GetType().FullName,
								enabled = plugin.EnabledByDefault,
								overrideEnabled = true,
							};
							overridePlugin = newPlugin;
							t._importPlugins.Add(newPlugin);
						}
					}
					EditorUtility.SetDirty(t);
				}
				EditorGUI.BeginDisabledGroup(!hasOverride);
				var currentlyEnabled = (overridePlugin != null && overridePlugin.overrideEnabled) ? overridePlugin.enabled : plugin.EnabledByDefault;
				var enabled = EditorGUILayout.ToggleLeft("", currentlyEnabled, GUILayout.Width(16));
				if (enabled != currentlyEnabled)
				{
					currentlyEnabled = enabled;
					overridePlugin.enabled = enabled;
					EditorUtility.SetDirty(t);
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(false);
				EditorGUILayout.LabelField(plugin.DisplayName);
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();
				// This assumes that the display name of a Plugin matches what extension it operates on.
				// It's not correct for Plugins that operate on multiple extensions, or none at all.
				if (hasWarning && t._extensions?.Find(x => x.name == plugin.DisplayName) != null)
				{
					EditorGUILayout.HelpBox(plugin.Warning, MessageType.Warning);
					editorCache.TryGetValue(plugin.GetType(), out var editor);
					CreateCachedEditor(plugin, null, ref editor);
					editor.OnInspectorGUI();
				}
			}
		}
		
		private static Dictionary<Type, Editor> editorCache = new Dictionary<Type, Editor>();

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
				if (x.texture && AssetDatabase.Contains(x.texture))
				{
					var path = AssetDatabase.GetAssetPath(x.texture);
					var asset = AssetImporter.GetAtPath(path);
					if (asset is TextureImporter textureImporter)
					{
						var correctLinearSetting = textureImporter.sRGBTexture == !x.shouldBeLinear;
						
						var correctNormalSetting = textureImporter.textureType == TextureImporterType.NormalMap == x.shouldBeNormalMap;
						return correctLinearSetting && correctNormalSetting;
					}
#if HAVE_KTX			
					else
					if (KtxImporterHelper.IsKtxOrBasis(asset))
					{
						if (!KtxImporterHelper.TryGetLinear(asset, out var linear))
							return false;
						
						var correctLinearSetting = (linear == x.shouldBeLinear | x.shouldBeNormalMap);
						return correctLinearSetting;
					}
#endif
				}
				return true;
			});
		}

		public static void FixTextureImportSettings(GLTFImporter importer)
		{
			var haveStartedAssetEditing = false;
			foreach (var x in importer.Textures)
			{
				if (!x.texture) continue;
				if (!AssetDatabase.Contains(x.texture))
					continue;
				
				var asset = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(x.texture));
#if HAVE_KTX
				if (KtxImporterHelper.IsKtxOrBasis(asset))
				{
					if (!KtxImporterHelper.TryGetLinear(asset, out var isLinear))
						continue;
					
					bool shouldBeLinear = x.shouldBeLinear | x.shouldBeNormalMap;
					if (isLinear == shouldBeLinear)
						continue;
		
					if (!haveStartedAssetEditing)
					{
						haveStartedAssetEditing = true;
						AssetDatabase.StartAssetEditing();
					}					
					
					KtxImporterHelper.SetLinear(asset, shouldBeLinear);
					
					asset.SaveAndReimport();
					continue;
				}
#endif
				
				if (!(asset is TextureImporter textureImporter)) continue;

				var correctLinearSetting = textureImporter.sRGBTexture == !x.shouldBeLinear;
				var correctNormalSetting = textureImporter.textureType == TextureImporterType.NormalMap == x.shouldBeNormalMap;
				
				// skip if already correct
				if (correctLinearSetting && correctNormalSetting)
					continue;

				if (!haveStartedAssetEditing)
				{
					haveStartedAssetEditing = true;
					AssetDatabase.StartAssetEditing();
				}

				textureImporter.sRGBTexture = !x.shouldBeLinear;
				textureImporter.textureType = x.shouldBeNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
				textureImporter.SaveAndReimport();
			}

			if (haveStartedAssetEditing)
				AssetDatabase.StopAssetEditing();
		}
	}
}
#endif
