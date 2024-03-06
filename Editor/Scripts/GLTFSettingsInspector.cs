#if UNITY_EDITOR && UNITY_IMGUI
#define SHOW_SETTINGS_EDITOR
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityGLTF.Cache;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
#if SHOW_SETTINGS_EDITOR
	internal class GltfSettingsProvider : SettingsProvider
	{
		internal static Action<GLTFSettings> OnAfterGUI;
		private static GLTFSettings settings;
		private SerializedProperty showDefaultReferenceNameWarning, showNamingRecommendationHint;

		public override void OnGUI(string searchContext)
		{
			if (!settings)
			{
				settings = GLTFSettings.GetOrCreateSettings();
				m_SerializedObject = new SerializedObject(settings);
			}
			DrawGLTFSettingsGUI(settings, m_SerializedObject);
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			base.OnActivate(searchContext, rootElement);
			CalculateCacheStats();
		}

		[SettingsProvider]
		public static SettingsProvider CreateGltfSettingsProvider()
		{
			GLTFSettings.GetOrCreateSettings();
			return new GltfSettingsProvider("Project/UnityGLTF", SettingsScope.Project);
		}

		public GltfSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
		{
		}

		private static long exportCacheByteLength = 0;

		private static void CalculateCacheStats()
		{
			var files = new List<FileInfo>();
			exportCacheByteLength = ExportCache.CalculateCacheSize(files);
		}

		private static SerializedObject m_SerializedObject;
		private static int m_ActiveEditorIndex = 0;
		private static readonly string[] m_TabNames = new string[2] { "Export", "Import" };
		private static readonly string key = typeof(GLTFSettings) + "ActiveEditorIndex";
		
		internal static void DrawGLTFSettingsGUI(GLTFSettings settings, SerializedObject m_SerializedObject)
		{
			EditorGUIUtility.labelWidth = 220;
			m_SerializedObject.Update();
			
			using (new GUILayout.HorizontalScope(Array.Empty<GUILayoutOption>()))
			{
				GUILayout.FlexibleSpace();
				m_ActiveEditorIndex = EditorPrefs.GetInt(key, 0);
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					m_ActiveEditorIndex = GUILayout.Toolbar(m_ActiveEditorIndex, m_TabNames, (GUIStyle) "LargeButton", GUI.ToolbarButtonSize.FitToContents);
					if (changeCheckScope.changed)
					{
						EditorPrefs.SetInt(key, m_ActiveEditorIndex);
					}
				}
				GUILayout.FlexibleSpace();
			}

			if (m_ActiveEditorIndex == 1)
			{
				var tooltip = "These plugins are enabled by default when importing a glTF file at runtime.\nFor assets imported in the editor, adjust plugin settings on the respective importer.";
				EditorGUILayout.LabelField(new GUIContent("Import Extensions and Plugins"), EditorStyles.boldLabel);
				EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
				EditorGUILayout.Space();
				OnPluginsGUI(settings.ImportPlugins);
				EditorGUILayout.Space();
			}
			else if (m_ActiveEditorIndex == 0)
			{
				var tooltip = "These plugins are enabled by default when exporting a glTF file. When using the export API, you can override which plugins are used.";
				EditorGUILayout.LabelField(new GUIContent("Export Extensions and Plugins"), EditorStyles.boldLabel);
				EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
				EditorGUILayout.Space();
				OnPluginsGUI(settings.ExportPlugins);
				EditorGUILayout.Space();
				
				var prop = m_SerializedObject.GetIterator();
				prop.NextVisible(true);
				if (prop.NextVisible(true))
				{
					do
					{
						EditorGUILayout.PropertyField(prop, true);
						switch (prop.name)
						{
							case nameof(GLTFSettings.UseCaching):
								EditorGUILayout.BeginHorizontal();
								EditorGUILayout.PrefixLabel(new GUIContent(" "));
								EditorGUILayout.BeginVertical();
								if (GUILayout.Button($"Clear Cache ({(exportCacheByteLength / (1024f * 1024f)):F2} MB)"))
								{
									ExportCache.Clear();
									CalculateCacheStats();
								}

								if (GUILayout.Button("Open Cache Directory ↗"))
									ExportCache.OpenCacheDirectory();
								EditorGUILayout.EndVertical();
								EditorGUILayout.EndHorizontal();
								break;
						}
					}
					while (prop.NextVisible(false));
				}
				EditorGUILayout.Space();

				if (m_SerializedObject.hasModifiedProperties)
				{
					m_SerializedObject.ApplyModifiedProperties();
				}
			}

			// Only for testing - all extension registry items should also show up via Plugins above
			/*
			EditorGUILayout.LabelField("Registered Deserialization Extensions", EditorStyles.boldLabel);
			// All plugins in the extension factory are supported for import.
			foreach (var ext in GLTFProperty.RegisteredExtensions)
			{
				EditorGUILayout.ToggleLeft(ext, true);
			}
			*/
			OnAfterGUI?.Invoke(settings);
		}

		private static Dictionary<Type, Editor> editorCache = new Dictionary<Type, Editor>();

		internal static void OnPluginsGUI(IEnumerable<GLTFPlugin> plugins, bool allowDisabling = true)
		{
			var lastAssembly = "";
			foreach (var plugin in plugins
				         .OrderBy(x =>
				         {
					         var displayName = x.GetType().Assembly.GetName().Name;
					         if (displayName == "UnityGLTFScripts") displayName = "____";
					         return displayName;
				         })
				         .ThenBy(x => x ? x.DisplayName : "ZZZ"))
			{
				if (!plugin) continue;
				var pluginAssembly = plugin.GetType().Assembly.GetName().Name;
				if (pluginAssembly == "UnityGLTFScripts") pluginAssembly = "UnityGLTF";
				if (lastAssembly != pluginAssembly)
				{
					lastAssembly = pluginAssembly;
					EditorGUILayout.Space();
					EditorGUILayout.LabelField(new GUIContent(pluginAssembly), EditorStyles.miniLabel);
				}
				
				EditorGUI.indentLevel++;
				var displayName = plugin.DisplayName ?? plugin.name;
				if (string.IsNullOrEmpty(displayName))
					displayName = ObjectNames.NicifyVariableName(plugin.GetType().Name);
				var key = plugin.GetType().FullName + "_SettingsExpanded";
				var expanded = SessionState.GetBool(key, false);
				using (new GUILayout.HorizontalScope())
				{
					if (plugin.AlwaysEnabled || !allowDisabling)
					{
						plugin.Enabled = true;
						// EditorGUI.BeginDisabledGroup(true);
						// GUILayout.Toggle(true, new GUIContent("", "Always enabled."), GUILayout.Width(12));
						// EditorGUI.EndDisabledGroup();
						if (allowDisabling)
							GUILayout.Label(GUIContent.none, GUILayout.Width(11));
					}
					else
					{
						plugin.Enabled = GUILayout.Toggle(plugin.Enabled, "", GUILayout.Width(12));
					}

					var label = new GUIContent(displayName, plugin.Description);
					EditorGUI.BeginDisabledGroup(!plugin.Enabled);
					var expanded2 = EditorGUILayout.Foldout(expanded, label);
					var lastFoldoutRect = GUILayoutUtility.GetLastRect();
					// check for right click so we can show a context menu
					EditorGUI.EndDisabledGroup();
					if (Event.current.type == EventType.MouseDown && lastFoldoutRect.Contains(Event.current.mousePosition))
					{
						if (Event.current.button == 0)
						{
							expanded2 = !expanded2;
						}
						else if (Event.current.button == 1)
						{
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Ping Script"), false, () => {
								var script = MonoScript.FromScriptableObject(plugin);
								EditorGUIUtility.PingObject(script);
							});
							menu.AddItem(new GUIContent("Edit Script"), false, () => {
								var script = MonoScript.FromScriptableObject(plugin);
								AssetDatabase.OpenAsset(script);
							});
							menu.ShowAsContext();
							Event.current.Use();
						}
					}
					
					EditorGUI.BeginDisabledGroup(!plugin.Enabled);
					if (plugin.Enabled && !string.IsNullOrEmpty(plugin.Warning))
					{
						// calculate space that the label needed
						var labelSize = EditorStyles.foldout.CalcSize(label);
						var warningIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
						warningIcon.tooltip = plugin.Warning;
						// show warning if needed
						var lastRect = GUILayoutUtility.GetLastRect();
						var warningRect = new Rect(lastRect.x + labelSize.x + 4, lastRect.y, 32, 16);
						EditorGUI.LabelField(warningRect, warningIcon);
					}

					EditorGUI.EndDisabledGroup();
					if (expanded2 != expanded)
					{
						expanded = expanded2;
						SessionState.SetBool(key, expanded2);
					}
				}

				if (expanded)
				{
					EditorGUI.indentLevel += 1;
					EditorGUILayout.HelpBox(plugin.Description, MessageType.None);
					if (!string.IsNullOrEmpty(plugin.Warning))
						EditorGUILayout.HelpBox(plugin.Warning, MessageType.Info);
					EditorGUI.BeginDisabledGroup(!plugin.Enabled);
					editorCache.TryGetValue(plugin.GetType(), out var editor);
					Editor.CreateCachedEditor(plugin, null, ref editor);
					editorCache[plugin.GetType()] = editor;
					editor.OnInspectorGUI();
					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel -= 1;
				}

				GUILayout.Space(2);

				EditorGUI.indentLevel--;
			}
		}

	}

	[CustomEditor(typeof(GLTFSettings))]
	internal class GLTFSettingsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			GLTFSettings.GetOrCreateSettings();
			GltfSettingsProvider.DrawGLTFSettingsGUI(target as GLTFSettings, serializedObject);
		}
	}
#endif
}