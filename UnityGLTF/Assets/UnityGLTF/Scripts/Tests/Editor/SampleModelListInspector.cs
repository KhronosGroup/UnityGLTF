using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Loader;

namespace UnityGLTF
{
	[CustomEditor(typeof(SampleModelList))]
	public class SampleModelListInspector : Editor
	{
		private List<SampleModel> models = null;
		private bool requestedModelList = false;
		private Vector2 scrollPosition = Vector2.zero;
		private SampleModel currentModel = null;

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();

			if (!Application.isPlaying)
			{
				models = null;
				requestedModelList = false;
				scrollPosition = Vector2.zero;
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
					using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
					{
						scrollPosition = scrollView.scrollPosition;

						foreach (var model in models)
						{
							DrawModel(model);
						}
					}
				}
			}
		}

		private void DrawModel(SampleModel model)
		{
			using (var horizontal = new EditorGUILayout.HorizontalScope())
			{
				GUIStyle style = new GUIStyle(GUI.skin.label);
				if (model == currentModel)
				{
					style.fontStyle = FontStyle.Bold;
				}
				GUILayout.Label(model.Name, style);

				foreach (var variant in model.Variants)
				{
					var buttonPressed = GUILayout.Button(variant.Name);

					if (buttonPressed)
					{
						currentModel = model;
						LoadModel(variant.ModelFilePath);
					}
				}
			}
		}

		private void LoadModel(string relativePath)
		{
			serializedObject.FindProperty(SampleModelList.ModelRelativePathFieldName).stringValue = relativePath;
			serializedObject.FindProperty(SampleModelList.LoadThisFrameFieldName).boolValue = true;
			serializedObject.ApplyModifiedProperties();
		}

		private async void DownloadSampleModelList()
		{
			var pathRoot = serializedObject.FindProperty(SampleModelList.PathRootFieldName).stringValue;
			var manifestRelativePath = serializedObject.FindProperty(SampleModelList.ManifestRelativePathFieldName).stringValue;

			var loader = new WebRequestLoader(pathRoot);
			try
			{
				await loader.LoadStream(manifestRelativePath);
			}
			catch (HttpRequestException)
			{
				Debug.LogError($"Failed to download sample model list manifest from: {pathRoot}{manifestRelativePath}", serializedObject.targetObject);
				throw;
			}

			loader.LoadedStream.Seek(0, SeekOrigin.Begin);

			var streamReader = new StreamReader(loader.LoadedStream);

			var reader = new JsonTextReader(streamReader);

			reader.Read();
			models = SampleModelListParser.ParseSampleModels(reader);
		}
	}
}
