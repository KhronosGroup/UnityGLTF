#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;

using UnityEngine;
using UnityEngine.Rendering;
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

			AddTab(new GLTFAssetImporterTab(this, "Extensions", ExtensionInspectorGUI));
			AddTab(new GLTFAssetImporterTab(this, "Info", AssetInfoInspectorGUI));

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
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._deduplicateResources)));
			if (t._deduplicatedStatistics != null && t._deduplicateResources != DeduplicateOptions.None)
			{
				var stats = t._deduplicatedStatistics;
				var meshText = t._deduplicateResources.HasFlag(DeduplicateOptions.Meshes)
					? $"Removed {stats.MeshesRemoved} Meshes ({stats.meshCountAfter}/{stats.meshCountBefore}). "
					: "";
				var textureText = t._deduplicateResources.HasFlag(DeduplicateOptions.Textures)
					? $"Removed {stats.TexturesRemoved} Textures  ({stats.textureCountAfter}/{stats.textureCountBefore}). "
					: "";
				EditorGUILayout.LabelField(" ", meshText+textureText, EditorStyles.miniLabel);
				//EditorGUILayout.HelpBox($"Result: "+meshText+textureText, MessageType.None);
			}
			// EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._maximumLod)), new GUIContent("Maximum Shader LOD"));
			EditorGUILayout.Separator();
			
			EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._readWriteEnabled)), new GUIContent("Read/Write"));

#pragma warning disable 0618	
			if (t._generateColliders)
			{
				serializedObject.FindProperty(nameof(GLTFImporter._addColliders)).enumValueIndex =
					(int)GLTFSceneImporter.ColliderType.Mesh;
				serializedObject.FindProperty(nameof(GLTFImporter._generateColliders)).boolValue = false;
			}
#pragma warning restore 0618
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._addColliders)));
			
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
				EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._texturesReadWriteEnabled)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._generateMipMaps)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._textureCompression)));
			}
		}

		private const string TextureRemappingKey = nameof(GLTFImporterInspector) + "_TextureRemapping";
		private bool EnableTextureRemapping
		{
#if UNITY_6000_4_OR_NEWER
			get => SessionState.GetBool(TextureRemappingKey + target.GetEntityId(), false);
			set => SessionState.SetBool(TextureRemappingKey + target.GetEntityId(), value);
#else
			get => SessionState.GetBool(TextureRemappingKey + target.GetInstanceID(), false);
			set => SessionState.SetBool(TextureRemappingKey + target.GetInstanceID(), value);
#endif
		}
		private static readonly GUIContent RemapTexturesToggleContent = new GUIContent("Experimental", "(experimental) Remap textures inside the glTF to textures that are already in your project.");

		private static readonly GUIContent PerClipLoopSettingsContent = new GUIContent("Loop Settings Per Clip",
			"Enable to set Loop Time and Loop Pose for each animation clip separately in the Animations list below. Clips start out with the settings above; while this is off, all clips use them.");

		private static readonly GUIContent AnimationsListContent = new GUIContent("Animations");

		/// <summary>
		/// Draws the list of imported animation clips. Same as drawing the array property directly, but with a
		/// header for the per-clip columns – and without the array size field, since the list can't be edited.
		/// </summary>
		private static void AnimationClipListGUI(SerializedProperty animations)
		{
			animations.isExpanded = EditorGUILayout.Foldout(animations.isExpanded, AnimationsListContent, true);
			if (!animations.isExpanded) return;

			EditorGUI.indentLevel++;
			AnimationClipImportInfoDrawer.HeaderGUI(animations);
			for (var i = 0; i < animations.arraySize; i++)
				EditorGUILayout.PropertyField(animations.GetArrayElementAtIndex(i));
			EditorGUI.indentLevel--;
		}

		/// <summary>
		/// Copies the importer-wide loop settings to all clips. Called when per-clip editing is turned on, so that
		/// the values shown in the list are the ones that are currently applied to the clips.
		/// </summary>
		private static void CopyLoopSettingsToClips(SerializedProperty animations, bool loopTime, bool loopPose)
		{
			if (animations == null) return;

			for (var i = 0; i < animations.arraySize; i++)
			{
				var clipInfo = animations.GetArrayElementAtIndex(i);
				clipInfo.FindPropertyRelative(nameof(GLTFImporter.AnimationClipImportInfo.loopTime)).boolValue = loopTime;
				clipInfo.FindPropertyRelative(nameof(GLTFImporter.AnimationClipImportInfo.loopPose)).boolValue = loopPose;
			}
		}

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
			if (anim.enumValueIndex == (int)AnimationMethod.MecanimHumanoid)
			{
				var flip = serializedObject.FindProperty(nameof(GLTFImporter._mecanimHumanoidFlip));
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(flip, new GUIContent("Flip Forward", "Some formats like VRM have a different forward direction for Avatars. Enable this option if the animation looks inverted."));
				EditorGUI.indentLevel--;
			}

			var animations = serializedObject.FindProperty(GLTFImporter.AnimationsPropertyName);
			if (hasAnimationData && anim.enumValueIndex > 0)
			{
				var loopTime = serializedObject.FindProperty(nameof(GLTFImporter._animationLoopTime));
				var loopPose = serializedObject.FindProperty(nameof(GLTFImporter._animationLoopPose));
				var perClipLoopSettings = serializedObject.FindProperty(nameof(GLTFImporter._animationLoopSettingsPerClip));

				// when set per clip, the loop settings are edited in the Animations list below
				if (!perClipLoopSettings.boolValue)
				{
					EditorGUILayout.PropertyField(loopTime, new GUIContent("Loop Time"));
					if (loopTime.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(loopPose, new GUIContent("Loop Pose"));
						EditorGUI.indentLevel--;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(perClipLoopSettings, PerClipLoopSettingsContent);
				if (EditorGUI.EndChangeCheck() && perClipLoopSettings.boolValue)
				{
					CopyLoopSettingsToClips(animations, loopTime.boolValue, loopPose.boolValue);
					animations.isExpanded = true; // the settings are edited there now, so make sure the list is visible
				}
			}

			// show animations for clip import editing
			if (animations.arraySize > 0)
			{
				EditorGUILayout.Space();
				AnimationClipListGUI(animations);
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
			if (importMaterialsProp.boolValue && GraphicsSettings.currentRenderPipeline)
			{
				EditorGUI.indentLevel++;
				EditorGUI.BeginDisabledGroup(!GraphicsSettings.currentRenderPipeline);
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._enableGpuInstancing)), new GUIContent("GPU Instancing"));
				EditorGUI.EndDisabledGroup();
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

				void ExtractAssets(T[] subAssets, bool importImmediately)
				{
					var assetPath = AssetDatabase.GetAssetPath(subAssets[0]);
					var assetImporter = AssetImporter.GetAtPath(assetPath);
					foreach (var subAsset in subAssets)
					{
						if (!subAsset) return;
						var filename = SanitizePath(subAsset.name);
						var dirName = Path.GetDirectoryName(t.assetPath) + "/" + subDirectoryName;
						if (!Directory.Exists(dirName))
							Directory.CreateDirectory(dirName);
						var destinationPath = dirName + "/" + filename + fileExtension;

						var clone = Instantiate(subAsset);
						AssetDatabase.CreateAsset(clone, destinationPath);

						assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);
					}

					if (importImmediately)
					{
						AssetDatabase.WriteImportSettingsIfDirty(assetPath);
						AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
					}
				}
				
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
					EditorGUILayout.BeginHorizontal();

					using (new EditorGUI.DisabledScope(remapCount == 0))
					{
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
					}

					if (typeof(T) == typeof(Material) && GUILayout.Button("Extract all " + subDirectoryName))
					{
						var materials = new T[importedData.arraySize];
						for (var i = 0; i < importedData.arraySize; i++)
							materials[i] = importedData.GetArrayElementAtIndex(i).objectReferenceValue as T;

						var extract = materials.Where(m => m != null).ToArray();
						AssetDatabase.StartAssetEditing();
						ExtractAssets(extract, false);
						AssetDatabase.StopAssetEditing();
						var assetPath = AssetDatabase.GetAssetPath(target);
						AssetDatabase.WriteImportSettingsIfDirty(assetPath);
						AssetDatabase.Refresh();
						return;
					}

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();


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

		private static GUIStyle _richTextWordWrap;
		private void AssetInfoInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) return;
			var assetProp = serializedObject.FindProperty(nameof(GLTFImporter._gltfAsset));
			if (assetProp == null)
				return;

			if (_richTextWordWrap == null)
			{
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.richText = true;
				style.wordWrap = true;
				_richTextWordWrap = style;
			}
			
			if (string.IsNullOrEmpty(t._gltfAsset))
			{
				EditorGUILayout.LabelField("<i>No asset information included in file</i>", _richTextWordWrap);
				return;
			}
			
			EditorGUILayout.Space();
			var rect = GUILayoutUtility.GetRect(new GUIContent(t._gltfAsset), _richTextWordWrap);
			EditorGUI.SelectableLabel(rect, t._gltfAsset, _richTextWordWrap);
			
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(true);
			var mainAssetIdentifierProp = serializedObject.FindProperty(nameof(GLTFImporter._mainAssetIdentifier));
			EditorGUILayout.PropertyField(mainAssetIdentifierProp);
		}

		private void ExtensionInspectorGUI()
		{
			var t = target as GLTFImporter;
			if (!t) return;

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._extensions)), new GUIContent("Extensions in file"));
			EditorGUI.EndDisabledGroup();

			// TODO add list of supported extensions and links to docs
			// Gather list of all plugins
			var registeredPlugins = GLTFSettings.GetDefaultSettings().ImportPlugins;
			var overridePlugins = t._importPlugins;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Available Import Plugins", EditorStyles.boldLabel);
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

			return sb.ToString().TrimStart(' ');
		}

		private static Editor cachedMateriaLibraryEditor;
		public override void DrawPreview(Rect previewArea)
		{
			// Is the root object a MaterialLibrary? Then draw the preview of that.
			// Otherwise, use base implementation.
			// get the assetimporter target object:
			if (assetTarget is MaterialLibrary materialLibrary)
			{
				var subassets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(materialLibrary)).Where(x => x is Material).ToArray();
				CreateCachedEditor(subassets, typeof(MaterialEditor), ref cachedMateriaLibraryEditor);
				cachedMateriaLibraryEditor.DrawPreview(previewArea);
			}
			else
			{
				base.DrawPreview(previewArea);
			}
		}

		protected override bool useAssetDrawPreview
		{
			get
			{
				if (assetTarget is MaterialLibrary)
					return false;
				return true;
			}
		}
	}

	/// <summary>
	/// Draws a single entry of the importer's animation clip list: the clip name, and – when
	/// "Loop Settings Per Clip" is enabled – the Loop Time / Loop Pose settings for that clip.
	/// </summary>
	[CustomPropertyDrawer(typeof(GLTFImporter.AnimationClipImportInfo))]
	internal class AnimationClipImportInfoDrawer : PropertyDrawer
	{
		private const string LoopTimeTooltip = "Make this clip loop seamlessly.";
		private const string LoopPoseTooltip = "Blend the pose of the last frame into the first frame of this clip.";

		private static readonly GUIContent ClipHeaderContent = new GUIContent("Clip");
		private static readonly GUIContent LoopTimeHeaderContent = new GUIContent("Loop Time", LoopTimeTooltip);
		private static readonly GUIContent LoopPoseHeaderContent = new GUIContent("Loop Pose", LoopPoseTooltip);
		private static readonly GUIContent LoopTimeToggleContent = new GUIContent("", LoopTimeTooltip);
		private static readonly GUIContent LoopPoseToggleContent = new GUIContent("", LoopPoseTooltip);

		private const float ColumnWidth = 72f;
		private const float SeparatorHeight = 1f;

		private static readonly Color SeparatorColor = EditorGUIUtility.isProSkin
			? new Color(1f, 1f, 1f, 0.1f)
			: new Color(0f, 0f, 0f, 0.15f);

		/// <summary>
		/// Draws the column header for the per-clip settings. Does nothing when the loop settings aren't set per clip,
		/// since then the list only shows the clip names.
		/// </summary>
		internal static void HeaderGUI(SerializedProperty animations)
		{
			if (!PerClipLoopSettingsEnabled(animations)) return;

			var position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + SeparatorHeight);
			using (new IndentScope(ref position))
			{
				var contentRect = DrawSeparator(position);
				GetColumnRects(contentRect, out var nameRect, out var loopTimeRect, out var loopPoseRect);
				EditorGUI.LabelField(nameRect, ClipHeaderContent, EditorStyles.miniBoldLabel);
				EditorGUI.LabelField(loopTimeRect, LoopTimeHeaderContent, EditorStyles.miniBoldLabel);
				EditorGUI.LabelField(loopPoseRect, LoopPoseHeaderContent, EditorStyles.miniBoldLabel);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight + SeparatorHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var nameProperty = property.FindPropertyRelative(nameof(GLTFImporter.AnimationClipImportInfo.name));
			var clipName = nameProperty != null && !string.IsNullOrEmpty(nameProperty.stringValue)
				? nameProperty.stringValue
				: label.text;
			var clipLabel = new GUIContent(clipName, clipName);

			var loopTime = property.FindPropertyRelative(nameof(GLTFImporter.AnimationClipImportInfo.loopTime));
			var loopPose = property.FindPropertyRelative(nameof(GLTFImporter.AnimationClipImportInfo.loopPose));
			// when the loop settings don't come from this clip there's nothing to edit here, only the name is shown
			var perClipLoopSettings = PerClipLoopSettingsEnabled(property) && loopTime != null && loopPose != null;

			using (new IndentScope(ref position))
			{
				var contentRect = DrawSeparator(position);
				if (!perClipLoopSettings)
				{
					EditorGUI.LabelField(contentRect, clipLabel);
					return;
				}

				GetColumnRects(contentRect, out var nameRect, out var loopTimeRect, out var loopPoseRect);
				EditorGUI.LabelField(nameRect, clipLabel);
				ToggleField(loopTimeRect, LoopTimeToggleContent, loopTime);
				using (new EditorGUI.DisabledScope(!loopTime.boolValue))
					ToggleField(loopPoseRect, LoopPoseToggleContent, loopPose);
			}
		}

		/// <summary>
		/// Draws the separator line at the bottom of a row and returns the remaining rect for the row content.
		/// </summary>
		private static Rect DrawSeparator(Rect position)
		{
			var separatorRect = new Rect(position.x, position.yMax - SeparatorHeight, position.width, SeparatorHeight);
			EditorGUI.DrawRect(separatorRect, SeparatorColor);

			position.height -= SeparatorHeight;
			return position;
		}

		private static bool PerClipLoopSettingsEnabled(SerializedProperty property)
		{
			var perClipLoopSettings = property.serializedObject.FindProperty(nameof(GLTFImporter._animationLoopSettingsPerClip));
			return perClipLoopSettings != null && perClipLoopSettings.boolValue;
		}

		private static void GetColumnRects(Rect position, out Rect nameRect, out Rect loopTimeRect, out Rect loopPoseRect)
		{
			nameRect = new Rect(position.x, position.y, Mathf.Max(position.width - ColumnWidth * 2f, 40f), position.height);
			loopTimeRect = new Rect(nameRect.xMax, position.y, ColumnWidth, position.height);
			loopPoseRect = new Rect(loopTimeRect.xMax, position.y, ColumnWidth, position.height);
		}

		private static void ToggleField(Rect rect, GUIContent content, SerializedProperty property)
		{
			// keep the clickable area at the checkbox, the columns are wider than that
			rect.width = EditorGUIUtility.singleLineHeight;

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			var value = EditorGUI.ToggleLeft(rect, content, property.boolValue);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
				property.boolValue = value;
		}

		/// <summary>
		/// Applies the current indentation to a rect and resets it, so that the columns can be laid out manually.
		/// </summary>
		private struct IndentScope : IDisposable
		{
			private readonly int _indentLevel;

			public IndentScope(ref Rect position)
			{
				position = EditorGUI.IndentedRect(position);
				_indentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
			}

			public void Dispose() => EditorGUI.indentLevel = _indentLevel;
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
