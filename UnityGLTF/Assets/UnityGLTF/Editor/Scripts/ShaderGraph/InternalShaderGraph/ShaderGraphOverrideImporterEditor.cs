#if !NO_INTERNALS_ACCESS && UNITY_2020_1_OR_NEWER

using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityGLTF
{
	[CustomEditor(typeof(ShaderGraphOverrideImporter))]
	class ShaderGraphOverrideImporterEditor : ScriptedImporterEditor
	{
		public override void OnInspectorGUI()
		{
#if UNITY_2021_1_OR_NEWER
			EditorGUILayout.HelpBox("Deprecated. Use material overrides on UnityGLTF/PBRGraph and UnityGLTF/UnlitGraph instead of separate shaders.", MessageType.Warning);
			if (GUILayout.Button("Update Project Materials"))
			{
				// reimport all materials
				var guids = AssetDatabase.FindAssets("t:Material");
				AssetDatabase.StartAssetEditing();
				foreach (var guid in guids)
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					AssetDatabase.ImportAsset(path);
				}
				AssetDatabase.StopAssetEditing();
			}
#endif
			DrawDefaultInspector();

			ApplyRevertGUI();
		}
	}
}

#endif
