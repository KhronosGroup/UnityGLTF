using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Loader;

[CustomEditor(typeof(SampleModelList))]
public class SampleModelListInspector : Editor
{
	private List<SampleModel> models = null;
	private bool requestedModelList = false;
	private Vector2 scroll = Vector2.zero;

	//private void OnEnable()
	//{
	//	Debug.Log("enable");
	//}

	//private void OnDisable()
	//{
	//	Debug.Log("disable");
	//}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.PropertyField(serializedObject.FindProperty(SampleModelList.LoaderFieldName));
		EditorGUILayout.PropertyField(serializedObject.FindProperty(SampleModelList.PathRootFieldName));
		EditorGUILayout.PropertyField(serializedObject.FindProperty(SampleModelList.ManifestRelativePathFieldName));
		EditorGUILayout.PropertyField(serializedObject.FindProperty(SampleModelList.ModelRelativePathFieldName));
		EditorGUILayout.PropertyField(serializedObject.FindProperty(SampleModelList.LoadThisFrameFieldName));

		if (!Application.isPlaying)
		{
			models = null;
			requestedModelList = false;
			scroll = Vector2.zero;
		}
		else
		{
			if (!requestedModelList)
			{
				requestedModelList = true;

				DownloadSampleModelList();
			}


			EditorGUILayout.LabelField("Models:");

			if (models != null)
			{
				scroll = EditorGUILayout.BeginScrollView(scroll);

				foreach (var model in models)
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField(model.Name);

					foreach (var variant in model.Variants)
					{
						var buttonPressed = GUILayout.Button(variant.Type);

						if (buttonPressed)
						{
							LoadModel(model.Name, variant.Type, variant.FileName);
						}
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.EndScrollView();
			}
		}
	}

	private void LoadModel(string modelName, string variantType, string variantName)
	{
		string relativePath = $"{modelName}/{variantType}/{variantName}";

		serializedObject.FindProperty(SampleModelList.ModelRelativePathFieldName).stringValue = relativePath;
		serializedObject.FindProperty(SampleModelList.LoadThisFrameFieldName).boolValue = true;
	}

	private async void DownloadSampleModelList()
	{
		var pathRoot = serializedObject.FindProperty(SampleModelList.PathRootFieldName).stringValue;
		var manifestRelativePath = serializedObject.FindProperty(SampleModelList.ManifestRelativePathFieldName).stringValue;

		var loader = new WebRequestLoader(pathRoot);
		await loader.LoadStream(manifestRelativePath);

		loader.LoadedStream.Seek(0, SeekOrigin.Begin);

		var streamReader = new StreamReader(loader.LoadedStream);

		//var s = streamReader.ReadToEnd();

		var reader = new JsonTextReader(streamReader);
		
		reader.Read();
		models = SampleModelListParser.ParseSampleModels(reader);
	}
}
