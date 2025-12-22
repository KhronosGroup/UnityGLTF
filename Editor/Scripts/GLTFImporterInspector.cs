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
        static Texture BoneAssignmentDotIcon;
        static Texture BoneAssignmentDotFrameIcon;
        static Texture BoneAssignmentDotFrameDottedIcon;

        static string[] boneGroupTabs = { "Body", "Head", "Left Hand", "Right Hand" };
        static new Dictionary<string, string> boneGroups = new()
        {
            { "Hips", "Body" },
            { "Spine", "Body" },
            { "Chest", "Body" },
            { "UpperChest", "Body" },
            { "Neck", "Body" },
            { "LeftUpperLeg", "Body" },
            { "RightUpperLeg", "Body" },
            { "LeftLowerLeg", "Body" },
            { "RightLowerLeg", "Body" },
            { "LeftFoot", "Body" },
            { "RightFoot", "Body" },
            { "LeftToes", "Body" },
            { "RightToes", "Body" },
            { "LeftShoulder", "Body" },
            { "LeftUpperArm", "Body" },
            { "LeftLowerArm", "Body" },
            { "LeftHand", "Body" },
            { "RightShoulder", "Body" },
            { "RightUpperArm", "Body" },
            { "RightLowerArm", "Body" },
            { "RightHand", "Body" },

            { "Head", "Head" },
            { "Jaw", "Head" },
            { "LeftEye", "Head" },
            { "RightEye", "Head" },

            { "Left Thumb Proximal", "Left Hand" },
            { "Left Thumb Intermediate", "Left Hand" },
            { "Left Thumb Distal", "Left Hand" },
            { "Left Index Proximal", "Left Hand" },
            { "Left Index Intermediate", "Left Hand" },
            { "Left Index Distal", "Left Hand" },
            { "Left Middle Proximal", "Left Hand" },
            { "Left Middle Intermediate", "Left Hand" },
            { "Left Middle Distal", "Left Hand" },
            { "Left Ring Proximal", "Left Hand" },
            { "Left Ring Intermediate", "Left Hand" },
            { "Left Ring Distal", "Left Hand" },
            { "Left Little Proximal", "Left Hand" },
            { "Left Little Intermediate", "Left Hand" },
            { "Left Little Distal", "Left Hand" },

            { "Right Thumb Proximal", "Right Hand" },
            { "Right Thumb Intermediate", "Right Hand" },
            { "Right Thumb Distal", "Right Hand" },
            { "Right Index Proximal", "Right Hand" },
            { "Right Index Intermediate", "Right Hand" },
            { "Right Index Distal", "Right Hand" },
            { "Right Middle Proximal", "Right Hand" },
            { "Right Middle Intermediate", "Right Hand" },
            { "Right Middle Distal", "Right Hand" },
            { "Right Ring Proximal", "Right Hand" },
            { "Right Ring Intermediate", "Right Hand" },
            { "Right Ring Distal", "Right Hand" },
            { "Right Little Proximal", "Right Hand" },
            { "Right Little Intermediate", "Right Hand" },
            { "Right Little Distal", "Right Hand" },
        };

        private string[] _importNormalsNames;
        GLTFImporter importer;
        readonly Dictionary<string, Transform> boneTransforms = new();
        readonly Dictionary<string, string> assignedBoneNames = new();
        Avatar avatar;
        int selectedBoneGroupTab = 0;

        public override void OnEnable()
		{
			if (!this) return;

            importer = target as GLTFImporter;

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

            BoneAssignmentDotIcon = (Texture)EditorGUIUtility.Load("DotFill");
            BoneAssignmentDotFrameIcon = (Texture)EditorGUIUtility.Load("DotFrame");
            BoneAssignmentDotFrameDottedIcon = (Texture)EditorGUIUtility.Load("DotFrameDotted");

            var animationMethod = serializedObject.FindProperty(nameof(GLTFImporter._importAnimations));
            if (animationMethod.enumValueIndex == (int)AnimationMethod.MecanimHumanoid)
            {
                avatar = AssetDatabase.LoadAssetAtPath<Avatar>(importer.assetPath);

                if (avatar && avatar.isHuman && avatar.isValid) PopulateBoneInfo();
            }

            base.OnEnable();
		}

        void PopulateBoneInfo()
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            boneTransforms.Clear();
            assignedBoneNames.Clear();

            if (modelAsset)
            {
                foreach (var transform in modelAsset.GetComponentsInChildren<Transform>(true))
                {
                    boneTransforms[transform.name] = transform;
                }

                foreach (var bone in avatar.humanDescription.human)
                {
                    assignedBoneNames[bone.humanName] = bone.boneName;
                }
            }
        }

        public override void OnInspectorGUI()
		{
			TextureWarningsGUI();
			EditorGUILayout.Space();
			base.OnInspectorGUI();
		}

		private void TextureWarningsGUI()
		{
			if (!importer)	return;
			if (!GLTFImporterHelper.TextureImportSettingsAreCorrect(importer))
			{
				EditorGUILayout.HelpBox("Some Textures have incorrect linear/sRGB settings. Results might be incorrect.", MessageType.Warning);
				if (GUILayout.Button("Fix All"))
				{
					GLTFImporterHelper.FixTextureImportSettings(importer);
				}
			}
		}

		private void ModelInspectorGUI()
		{
			if (!importer) return;

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
			// EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._maximumLod)), new GUIContent("Maximum Shader LOD"));
			EditorGUILayout.Separator();
			
			EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._readWriteEnabled)), new GUIContent("Read/Write"));

#pragma warning disable 0618	
			if (importer._generateColliders)
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
			get => SessionState.GetBool(TextureRemappingKey + target.GetInstanceID(), false);
			set => SessionState.SetBool(TextureRemappingKey + target.GetInstanceID(), value);
		}
		private static readonly GUIContent RemapTexturesToggleContent = new GUIContent("Experimental", "(experimental) Remap textures inside the glTF to textures that are already in your project.");

        private void AnimationInspectorGUI()
        {
            if (!importer) return;

            var hasAnimationData = serializedObject.FindProperty(nameof(GLTFImporter.m_HasAnimationData)).boolValue;

            if (!hasAnimationData)
            {
                EditorGUILayout.HelpBox("File doesn't contain animation data.", MessageType.None);
            }

            var animationMethod = serializedObject.FindProperty(nameof(GLTFImporter._importAnimations));
            EditorGUILayout.PropertyField(animationMethod, new GUIContent("Animation Type"));
            if (animationMethod.enumValueIndex == (int)AnimationMethod.MecanimHumanoid)
            {
                var flip = serializedObject.FindProperty(nameof(GLTFImporter._mecanimHumanoidFlip));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(flip, new GUIContent("Flip Forward", "Some formats like VRM have a different forward direction for Avatars. Enable this option if the animation looks inverted."));
                EditorGUI.indentLevel--;
            }
            else if (animationMethod.enumValueIndex == (int)AnimationMethod.Mecanim)
            {
                var rootNodeName = serializedObject.FindProperty(nameof(GLTFImporter._nonHumanoidRootNodeName));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(rootNodeName, new GUIContent("Root Node Name", "Transform name of the root motion transform. If empty no root motion is defined and you must take care of avatar movement yourself."));
                EditorGUILayout.HelpBox("Enter just the name of the root node; not its path in hierarchy.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            if (hasAnimationData && animationMethod.enumValueIndex > 0)
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

            if (animationMethod.enumValueIndex == (int)AnimationMethod.MecanimHumanoid)
            {
                // List all bones and any assigned gameobjects.
                if (avatar && avatar.isHuman && avatar.isValid)
                {
                    var humanBones = avatar.humanDescription.human;
                    var allMecanimBonesCount = HumanTrait.BoneName.Length;
                    EditorGUILayout.Separator();
                    EditorGUILayout.LabelField("Avatar Bones", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Assigned Bones:", humanBones.Length + "/" + allMecanimBonesCount);

                    EditorGUILayout.BeginHorizontal();
                    var legendIconRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                    GUI.DrawTexture(legendIconRect, BoneAssignmentDotFrameDottedIcon, ScaleMode.ScaleToFit, true);
                    GUILayout.Label("Optional Bone", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    selectedBoneGroupTab = GUILayout.Toolbar(selectedBoneGroupTab, boneGroupTabs);

                    EditorGUI.indentLevel++;

                    for (var i = 0; i < allMecanimBonesCount; i++)
                    {
                        var mecanimBoneName = HumanTrait.BoneName[i];
                        if (boneGroupTabs[selectedBoneGroupTab] != boneGroups[mecanimBoneName])  continue;

                        assignedBoneNames.TryGetValue(mecanimBoneName, out string assignedBoneName);
                        Transform transform = null;
                        if (!string.IsNullOrEmpty(assignedBoneName))
                        {
                            boneTransforms.TryGetValue(assignedBoneName, out transform);
                        }

                        EditorGUILayout.BeginHorizontal();

                        var iconRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                        var frameIcon = HumanTrait.RequiredBone(i) ? BoneAssignmentDotFrameIcon : BoneAssignmentDotFrameDottedIcon;

                        if (transform)
                        {
                            var originalColor = GUI.color;
                            GUI.color = Color.green;
                            GUI.DrawTexture(iconRect, frameIcon, ScaleMode.ScaleToFit, true);
                            GUI.DrawTexture(iconRect, BoneAssignmentDotIcon, ScaleMode.ScaleToFit, true);
                            GUI.color = originalColor;
                        }
                        else GUI.DrawTexture(iconRect, frameIcon, ScaleMode.ScaleToFit, true);

                        GUILayout.Label(mecanimBoneName, GUILayout.Width(130));

                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(transform, typeof(Transform), true);
                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }
                // warn if Humanoid rig import has failed
                else EditorGUILayout.HelpBox("The model doesn't contain a valid Humanoid rig. See the console for more information.", MessageType.Error);
            }
        }

		private void MaterialInspectorGUI()
		{
			if (!importer) return;

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
				RemappingUI<Material>(importedMaterials, "Materials", ".mat");
			}

			// There's a bunch of known edge cases with texture remapping that can go wrong,
			// So it's disabled for now.
			var current = EnableTextureRemapping;
			var val = EditorGUILayout.Toggle(RemapTexturesToggleContent, EnableTextureRemapping);
			if (val != current) EnableTextureRemapping = val;
			var importedTextures = serializedObject.FindProperty("m_Textures");
			if (EnableTextureRemapping && importedTextures.arraySize > 0)
			{
				RemappingUI<Texture>(importedTextures, "Textures", ".asset");
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

		private void RemappingUI<T>(SerializedProperty importedData, string subDirectoryName, string fileExtension) where T: UnityEngine.Object
		{
			// extract and remap materials
			if (importedData != null && importedData.serializedObject != null)
			{
				EditorGUI.indentLevel++;

				var externalObjectMap = importer.GetExternalObjectMap();
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
						var dirName = Path.GetDirectoryName(importer.assetPath) + "/" + subDirectoryName;
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
					var dirName = Path.GetDirectoryName(importer.assetPath) + "/" + subDirectoryName;
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
                                importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(mat));
							}

							// also remove all old remaps
							var oldRemaps = externalObjectMap.Where(x => x.Value is T).ToList();
							foreach (var oldRemap in oldRemaps)
							{
                                importer.RemoveRemap(oldRemap.Key);
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
                                importer.AddRemap(id, newObj);
							else
                                importer.RemoveRemap(id);
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
                                importer.RemoveRemap(id);
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
			if (!importer) return;
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
			
			if (string.IsNullOrEmpty(importer._gltfAsset))
			{
				EditorGUILayout.LabelField("<i>No asset information included in file</i>", _richTextWordWrap);
				return;
			}
			
			EditorGUILayout.Space();
			var rect = GUILayoutUtility.GetRect(new GUIContent(importer._gltfAsset), _richTextWordWrap);
			EditorGUI.SelectableLabel(rect, importer._gltfAsset, _richTextWordWrap);
			
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(true);
			var mainAssetIdentifierProp = serializedObject.FindProperty(nameof(GLTFImporter._mainAssetIdentifier));
			EditorGUILayout.PropertyField(mainAssetIdentifierProp);
		}

		private void ExtensionInspectorGUI()
		{
			if (!importer) return;

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._extensions)), new GUIContent("Extensions in file"));
			EditorGUI.EndDisabledGroup();

			// TODO add list of supported extensions and links to docs
			// Gather list of all plugins
			var registeredPlugins = GLTFSettings.GetDefaultSettings().ImportPlugins;
			var overridePlugins = importer._importPlugins;

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
                            importer._importPlugins.Add(newPlugin);
						}
					}
					EditorUtility.SetDirty(importer);
				}
				EditorGUI.BeginDisabledGroup(!hasOverride);
				var currentlyEnabled = (overridePlugin != null && overridePlugin.overrideEnabled) ? overridePlugin.enabled : plugin.EnabledByDefault;
				var enabled = EditorGUILayout.ToggleLeft("", currentlyEnabled, GUILayout.Width(16));
				if (enabled != currentlyEnabled)
				{
					currentlyEnabled = enabled;
					overridePlugin.enabled = enabled;
					EditorUtility.SetDirty(importer);
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(false);
				EditorGUILayout.LabelField(plugin.DisplayName);
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();
				// This assumes that the display name of a Plugin matches what extension it operates on.
				// It's not correct for Plugins that operate on multiple extensions, or none at all.
				if (hasWarning && importer._extensions?.Find(x => x.name == plugin.DisplayName) != null)
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
