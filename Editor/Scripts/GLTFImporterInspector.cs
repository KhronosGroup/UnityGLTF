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
		static Dictionary<string, string> boneGroups = new()
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

		private GLTFImportInfo _importInfo;
		private bool _importInfoLoaded;
		private SerializedObject _serializedImportInfo;
		private List<GLTFImporter.ExtensionInfo> _extensionsOfFailedImport;

		/// <summary>The extensions in the file, also when the import failed.</summary>
		private List<GLTFImporter.ExtensionInfo> Extensions
		{
			get
			{
				if (ImportInfo) return ImportInfo.extensions;
				if (_extensionsOfFailedImport == null && importer)
					_extensionsOfFailedImport = GLTFImporter.ReadExtensions(importer.assetPath);
				return _extensionsOfFailedImport;
			}
		}

		/// <summary>
		/// The import results of the (first) selected asset, see <see cref="GLTFImportInfo"/>. Null when the asset
		/// wasn't imported (yet) or the import failed. Loaded again when the asset was reimported.
		/// </summary>
		private GLTFImportInfo ImportInfo
		{
			get
			{
				// a reimport replaces the sub-asset, the old one is destroyed then
				var destroyed = !ReferenceEquals(_importInfo, null) && !_importInfo;
				if ((!_importInfoLoaded || destroyed) && importer)
				{
					_importInfo = GLTFImportInfo.Load(importer.assetPath);
					_importInfoLoaded = true;
					_serializedImportInfo = null;
					_extensionsOfFailedImport = null;
				}
				return _importInfo;
			}
		}

		private SerializedObject SerializedImportInfo
		{
			get
			{
				var importInfo = ImportInfo;
				if (!importInfo) return null;
				if (_serializedImportInfo == null || _serializedImportInfo.targetObject != importInfo)
					_serializedImportInfo = new SerializedObject(importInfo);
				return _serializedImportInfo;
			}
		}

		public override void OnEnable()
		{
			if (!this) return;

			importer = target as GLTFImporter;
			_importInfoLoaded = false;

			// without import info (not imported yet, or the import failed) all tabs are shown
			var importInfo = ImportInfo;
			if (!importInfo || importInfo.hasSceneData)
				AddTab(new GLTFAssetImporterTab(this, "Model", ModelInspectorGUI));

			AddTab(new GLTFAssetImporterTab(this, "Animation", AnimationInspectorGUI));

			if (!importInfo || importInfo.hasMaterialData || importInfo.hasTextureData)
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
			if (!importer || !ImportInfo) return;
			if (!GLTFImporterHelper.TextureImportSettingsAreCorrect(ImportInfo.textureInfos))
			{
				EditorGUILayout.HelpBox("Some Textures have incorrect import settings (linear/sRGB, filter and wrap mode, compression of small textures). Results might be incorrect.", MessageType.Warning);
				if (GUILayout.Button("Fix All"))
				{
					GLTFImporterHelper.FixTextureImportSettings(ImportInfo.textureInfos);
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
			var stats = ImportInfo ? ImportInfo.deduplicatedStatistics : null;
			if (stats != null && importer._deduplicateResources != DeduplicateOptions.None)
			{
				var meshText = importer._deduplicateResources.HasFlag(DeduplicateOptions.Meshes)
					? $"Removed {stats.MeshesRemoved} Meshes ({stats.meshCountAfter}/{stats.meshCountBefore}). "
					: "";
				var textureText = importer._deduplicateResources.HasFlag(DeduplicateOptions.Textures)
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
			if (ImportInfo && ImportInfo.textures.Length > 0)
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

		private static readonly GUIContent RootNodeContent = new GUIContent("Root Node Name",
			"Transform name of the root motion transform. If empty no root motion is defined and you must take care of avatar movement yourself.");

		private GameObject _rootNodeCacheModel;
		private string _rootNodeCacheName;
		private int _rootNodeCacheMatches;
		private string _pendingRootNodeName;

		private const float RootNodeDropdownWidth = 18;

		/// <summary>
		/// Draws the root motion node as a text field, with a dropdown button next to it that shows the hierarchy of the
		/// imported model (with submenus for children) to pick a node from.
		/// Only the transform name is stored, since that's what the generic avatar is built from.
		/// </summary>
		private void RootNodeGUI(SerializedProperty rootNodeName)
		{
			var model = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
			if (!model || serializedObject.isEditingMultipleObjects)
			{
				// No imported hierarchy to pick from (or several importers selected) – only the text field
				EditorGUILayout.PropertyField(rootNodeName, RootNodeContent);
				return;
			}

			if (_pendingRootNodeName != null)
			{
				// Leave text editing first, otherwise a focused field keeps showing its edit buffer instead of the picked name
				GUI.FocusControl(null);
				EditorGUIUtility.editingTextField = false;
				rootNodeName.stringValue = _pendingRootNodeName;
				_pendingRootNodeName = null;
			}

			var rect = EditorGUILayout.GetControlRect();
			var fieldRect = new Rect(rect.x, rect.y, rect.width - RootNodeDropdownWidth - 2, rect.height);
			var buttonRect = new Rect(rect.xMax - RootNodeDropdownWidth, rect.y, RootNodeDropdownWidth, rect.height);

			EditorGUI.PropertyField(fieldRect, rootNodeName, RootNodeContent);

			var current = rootNodeName.stringValue;
			if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, EditorStyles.miniPullDown))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(current), () => SetRootNodeName(""));
				menu.AddSeparator("");
				foreach (Transform child in model.transform)
					AddRootNodeMenuItems(menu, child, "", current);
				menu.DropDown(buttonRect);
			}

			if (string.IsNullOrEmpty(current)) return;

			if (_rootNodeCacheModel != model || _rootNodeCacheName != current)
			{
				_rootNodeCacheModel = model;
				_rootNodeCacheName = current;
				// The avatar builder only searches below the root
				_rootNodeCacheMatches = model.GetComponentsInChildren<Transform>(true).Count(t => t != model.transform && t.name == current);
			}

			if (_rootNodeCacheMatches == 0)
				EditorGUILayout.HelpBox($"No transform named \"{current}\" found below the root of the imported hierarchy. The avatar will be built without a root motion node.", MessageType.Warning);
			else if (_rootNodeCacheMatches > 1)
				EditorGUILayout.HelpBox($"{_rootNodeCacheMatches} transforms are named \"{current}\". The root motion node is matched by name, so this is ambiguous – rename the nodes in the source file to make them unique.", MessageType.Warning);
		}

		private void AddRootNodeMenuItems(GenericMenu menu, Transform transform, string menuPath, string current)
		{
			// "/" in a name would create a submenu, so replace it with a look-alike character for display
			var displayName = string.IsNullOrEmpty(transform.name) ? "<unnamed>" : transform.name.Replace('/', '∕');
			var isSelected = transform.name == current;

			if (transform.childCount == 0)
			{
				AddRootNodeMenuItem(menu, menuPath + displayName, transform.name, isSelected);
				return;
			}

			// A node with children becomes a submenu; the node itself is the first entry inside it
			var subMenuPath = menuPath + displayName + "/";
			AddRootNodeMenuItem(menu, subMenuPath + displayName, transform.name, isSelected);
			menu.AddSeparator(subMenuPath);
			foreach (Transform child in transform)
				AddRootNodeMenuItems(menu, child, subMenuPath, current);
		}

		private void AddRootNodeMenuItem(GenericMenu menu, string menuPath, string nodeName, bool isSelected)
		{
			if (string.IsNullOrEmpty(nodeName))
				menu.AddDisabledItem(new GUIContent(menuPath));
			else
				menu.AddItem(new GUIContent(menuPath), isSelected, () => SetRootNodeName(nodeName));
		}

		/// <summary>
		/// Called from the dropdown menu, which runs outside of the inspector GUI pass.
		/// The value is applied in the next GUI pass, where the text field can be unfocused first.
		/// </summary>
		private void SetRootNodeName(string nodeName)
		{
			_pendingRootNodeName = nodeName;
			Repaint();
		}

		private void AnimationInspectorGUI()
		{
			if (!importer) return;

			// without import info (not imported yet, or the import failed) the animation settings are shown
			var hasAnimationData = !ImportInfo || ImportInfo.hasAnimationData;

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
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GLTFImporter._mecanimHumanoidClips)), new GUIContent("Convert Clips to Humanoid", "Convert the clips to humanoid clips (muscle curves), so they can be retargeted to other humanoids and the Animator extracts root motion from them. When off, the clips stay generic and only play on this hierarchy."));
				EditorGUI.indentLevel--;
			}
			else if (animationMethod.enumValueIndex == (int)AnimationMethod.Mecanim)
			{
				EditorGUI.indentLevel++;
				RootNodeGUI(serializedObject.FindProperty(nameof(GLTFImporter._nonHumanoidRootNodeName)));
				EditorGUILayout.HelpBox("Enter just the name of the root node; not its path in hierarchy.", MessageType.Info);
				EditorGUI.indentLevel--;
			}

			var animations = serializedObject.FindProperty(GLTFImporter.AnimationsPropertyName);
			if (hasAnimationData && animationMethod.enumValueIndex > 0)
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
			var importInfo = ImportInfo;
			if (importInfo && importInfo.materials.Length > 0)
			{
				RemappingUI<Material>(importInfo.materials, "Materials", ".mat");
			}

			// There's a bunch of known edge cases with texture remapping that can go wrong,
			// So it's disabled for now.
			var current = EnableTextureRemapping;
			var val = EditorGUILayout.Toggle(RemapTexturesToggleContent, EnableTextureRemapping);
			if (val != current) EnableTextureRemapping = val;
			if (EnableTextureRemapping && importInfo && importInfo.textures.Length > 0)
			{
				RemappingUI<Texture>(importInfo.textures, "Textures", ".asset");
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

		private void RemappingUI<T>(T[] importedData, string subDirectoryName, string fileExtension) where T: UnityEngine.Object
		{
			// extract and remap materials
			if (importedData != null)
			{
				EditorGUI.indentLevel++;

				var externalObjectMap = importer.GetExternalObjectMap();
				// TODO this also counts old remaps that are not used anymore
				var remapCount = externalObjectMap.Values.Count(x => x is T) + importer.RemapsInFile.Count(typeof(T));

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

						if (assetImporter is GLTFImporter gltfImporter) gltfImporter.SetRemap(subAsset, clone);
						else assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);
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
					if (assetImporter is GLTFImporter gltfImporter) gltfImporter.SetRemap(subAsset, clone);
					else assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);

					if (importImmediately)
					{
						AssetDatabase.WriteImportSettingsIfDirty(assetPath);
						AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
					}
				}

				var remapsWithMissingTarget = importer.GetRemapsWithMissingTarget(typeof(T));
				if (remapsWithMissingTarget.Count > 0)
				{
					EditorGUILayout.HelpBox($"The remap targets of {string.Join(", ", remapsWithMissingTarget.Select(x => x.name))} are missing. " +
					                        "They were deleted, or they are " + subDirectoryName.ToLowerInvariant() + " of this file remapped with an older UnityGLTF version, which doesn't work. " +
					                        "Remove these remaps and remap them again.", MessageType.Warning);
					if (GUILayout.Button("Remove remaps with missing target"))
						foreach (var remap in remapsWithMissingTarget)
							importer.RemoveRemap(remap);
				}

				string remapFoldoutKey = nameof(GLTFImporterInspector) + "_Remap_" + subDirectoryName + "_Foldout";
				var remapFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(SessionState.GetBool(remapFoldoutKey, false), "Remapped " + subDirectoryName + " (" + remapCount + " / " + importedData.Length + ")");
				SessionState.SetBool(remapFoldoutKey, remapFoldout);
				if (remapFoldout)
				{
					EditorGUILayout.BeginHorizontal();

					using (new EditorGUI.DisabledScope(remapCount == 0))
					{
						if (GUILayout.Button("Restore all " + subDirectoryName))
						{
							// also removes old remaps of sub-assets that don't exist anymore
							importer.RemoveAllRemaps(typeof(T));
						}
					}

					if (typeof(T) == typeof(Material) && GUILayout.Button("Extract all " + subDirectoryName))
					{
						var extract = importedData.Where(m => m != null).ToArray();
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


					for (var i = 0; i < importedData.Length; i++)
					{
						var mat = importedData[i];
						if (!mat) continue;
						var id = new AssetImporter.SourceAssetIdentifier(mat);
						externalObjectMap.TryGetValue(id, out var remap);
						// a remap to another sub-asset of this file is stored by name
						var targetNameInFile = remap ? null : importer.RemapsInFile.GetTargetName(mat);
						if (targetNameInFile != null)
							remap = Array.Find(importedData, x => x && x.name == targetNameInFile);
						EditorGUILayout.BeginHorizontal();
						// EditorGUILayout.ObjectField(/*mat.name,*/ mat, typeof(Material), false);
						EditorGUI.BeginChangeCheck();
						var newObj = EditorGUILayout.ObjectField(mat.name, remap, typeof(T), false);
						if (EditorGUI.EndChangeCheck())
						{
							importer.SetRemap(mat, newObj);
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
								importer.SetRemap(mat, null);
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
			var importInfo = ImportInfo;
			if (!importInfo)
			{
				EditorGUILayout.HelpBox("No import results, the asset wasn't imported successfully.", MessageType.None);
				return;
			}

			if (_richTextWordWrap == null)
			{
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.richText = true;
				style.wordWrap = true;
				_richTextWordWrap = style;
			}

			if (string.IsNullOrEmpty(importInfo.gltfAsset))
			{
				EditorGUILayout.LabelField("<i>No asset information included in file</i>", _richTextWordWrap);
				return;
			}

			EditorGUILayout.Space();
			var rect = GUILayoutUtility.GetRect(new GUIContent(importInfo.gltfAsset), _richTextWordWrap);
			EditorGUI.SelectableLabel(rect, importInfo.gltfAsset, _richTextWordWrap);

			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.TextField("Main Asset Identifier", importInfo.mainAssetIdentifier);
			EditorGUI.EndDisabledGroup();
		}

		private void ExtensionInspectorGUI()
		{
			if (!importer) return;

			var serializedImportInfo = SerializedImportInfo;
			EditorGUI.BeginDisabledGroup(true);
			if (serializedImportInfo != null)
			{
				serializedImportInfo.Update();
				EditorGUILayout.PropertyField(serializedImportInfo.FindProperty(nameof(GLTFImportInfo.extensions)), new GUIContent("Extensions in file"));
			}
			else
			{
				// the import failed, so the extensions come from the file
				EditorGUILayout.LabelField("Extensions in file", EditorStyles.boldLabel);
				foreach (var extension in Extensions ?? new List<GLTFImporter.ExtensionInfo>())
					EditorGUILayout.LabelField(extension.name, extension.required ? "required" : "");
			}
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
				if (hasWarning && Extensions?.Find(x => x.name == plugin.DisplayName) != null)
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
			return !importer || TextureImportSettingsAreCorrect(GLTFImportInfo.Load(importer.assetPath)?.textureInfos);
		}

		internal static bool TextureImportSettingsAreCorrect(List<GLTFImporter.TextureInfo> textureInfos)
		{
			if (textureInfos == null) return true;
			return textureInfos.All(x =>
			{
				if (x.texture && AssetDatabase.Contains(x.texture))
				{
					var path = AssetDatabase.GetAssetPath(x.texture);
					var asset = AssetImporter.GetAtPath(path);
					if (asset is TextureImporter textureImporter)
					{
						var correctLinearSetting = textureImporter.sRGBTexture == !x.shouldBeLinear;
						
						var correctNormalSetting = textureImporter.textureType == TextureImporterType.NormalMap == x.shouldBeNormalMap;
						return correctLinearSetting && correctNormalSetting && SamplerIsCorrect(textureImporter, x, textureInfos) && CompressionIsCorrect(textureImporter, x);
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
					else
					if (WebPImporterHelper.IsWebP(asset))
					{
						return !WebPImporterHelper.TryGetLinear(asset, out var linear) || linear == (x.shouldBeLinear || x.shouldBeNormalMap);
					}
				}
				return true;
			});
		}

		/// <summary>
		/// A texture file keeps its filter and wrap modes in its importer, not in the glTF. When glTF textures with different samplers share
		/// one file there is no right setting, so the file is left alone.
		/// </summary>
		private static bool SamplerIsCorrect(TextureImporter importer, GLTFImporter.TextureInfo info, List<GLTFImporter.TextureInfo> textureInfos)
		{
			if (importer.filterMode == info.filterMode && importer.wrapModeU == info.wrapModeU && importer.wrapModeV == info.wrapModeV) return true;
			return textureInfos.Any(other => other.texture == info.texture && (other.filterMode != info.filterMode || other.wrapModeU != info.wrapModeU || other.wrapModeV != info.wrapModeV));
		}

		/// <summary>Textures up to this size (width and height) are imported uncompressed, see <see cref="CompressionIsCorrect"/>.</summary>
		private const int SmallTextureSize = 128;

		/// <summary>
		/// Small textures are mostly color palettes or lookup tables, where every pixel matters. Block compression (DXT, ASTC, ...) keeps only a few
		/// colors per 4x4 block, which changes such textures visibly, while saving almost no memory. The textures embedded in a .glb or imported
		/// by the glTF importer itself are uncompressed, so this also makes a texture file look the same as the embedded one.
		/// </summary>
		private static bool CompressionIsCorrect(TextureImporter importer, GLTFImporter.TextureInfo info)
		{
			if (!info.texture || Mathf.Max(info.texture.width, info.texture.height) > SmallTextureSize) return true;
			return importer.textureCompression == TextureImporterCompression.Uncompressed;
		}

		public static void FixTextureImportSettings(GLTFImporter importer)
		{
			if (!importer) return;
			FixTextureImportSettings(GLTFImportInfo.Load(importer.assetPath)?.textureInfos);
		}

		internal static void FixTextureImportSettings(List<GLTFImporter.TextureInfo> textureInfos)
		{
			if (textureInfos == null) return;
			var haveStartedAssetEditing = false;
			foreach (var x in textureInfos)
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

				if (WebPImporterHelper.IsWebP(asset))
				{
					if (!WebPImporterHelper.TryGetLinear(asset, out var webPIsLinear))
						continue;

					var webPShouldBeLinear = x.shouldBeLinear || x.shouldBeNormalMap;
					if (webPIsLinear == webPShouldBeLinear)
						continue;

					if (!haveStartedAssetEditing)
					{
						haveStartedAssetEditing = true;
						AssetDatabase.StartAssetEditing();
					}

					WebPImporterHelper.SetLinear(asset, webPShouldBeLinear);
					asset.SaveAndReimport();
					continue;
				}

				if (!(asset is TextureImporter textureImporter)) continue;

				var correctLinearSetting = textureImporter.sRGBTexture == !x.shouldBeLinear;
				var correctNormalSetting = textureImporter.textureType == TextureImporterType.NormalMap == x.shouldBeNormalMap;
				
				var correctSampler = SamplerIsCorrect(textureImporter, x, textureInfos);
				var correctCompression = CompressionIsCorrect(textureImporter, x);

				// skip if already correct
				if (correctLinearSetting && correctNormalSetting && correctSampler && correctCompression)
					continue;

				if (!haveStartedAssetEditing)
				{
					haveStartedAssetEditing = true;
					AssetDatabase.StartAssetEditing();
				}

				if (!correctLinearSetting || !correctNormalSetting)
				{
					textureImporter.sRGBTexture = !x.shouldBeLinear;
					textureImporter.textureType = x.shouldBeNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
				}
				if (!correctCompression)
					textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
				if (!correctSampler)
				{
					textureImporter.filterMode = x.filterMode;
					textureImporter.wrapModeU = x.wrapModeU;
					textureImporter.wrapModeV = x.wrapModeV;
				}
				textureImporter.SaveAndReimport();
			}

			if (haveStartedAssetEditing)
				AssetDatabase.StopAssetEditing();
		}
	}
}
#endif
