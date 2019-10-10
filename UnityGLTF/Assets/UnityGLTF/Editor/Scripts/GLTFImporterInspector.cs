#if UNITY_2017_1_OR_NEWER
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityGLTF
{
	[CustomEditor(typeof(GLTFImporter))]
	[CanEditMultipleObjects]
	public class GLTFImporterInspector : AssetImporterEditor
	{
		private string[] _importNormalsNames;

		public override void OnInspectorGUI()
		{
			if (_importNormalsNames == null)
			{
				_importNormalsNames = Enum.GetNames(typeof(GLTFImporterNormals))
					.Select(n => ObjectNames.NicifyVariableName(n))
					.ToArray();
			}
			EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_removeEmptyRootObjects"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_scaleFactor"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_maximumLod"), new GUIContent("Maximum LOD"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_readWriteEnabled"), new GUIContent("Read/Write Enabled"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_generateColliders"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_swapUvs"), new GUIContent("Swap UVs"));
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Normals", EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();
			var importNormalsProp = serializedObject.FindProperty("_importNormals");
			var importNormals = EditorGUILayout.Popup(importNormalsProp.displayName, importNormalsProp.intValue, _importNormalsNames);
			if (EditorGUI.EndChangeCheck())
			{
				importNormalsProp.intValue = importNormals;
			}
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_importMaterials"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_useJpgTextures"), new GUIContent("Use JPG Textures"));

			ApplyRevertGUI();
		}
	}
}
#endif
