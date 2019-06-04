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

			bool enabled = true;
			var targetObject = serializedObject.targetObject as SampleModelList;

			if (targetObject != null)
			{
				enabled = targetObject.enabled;
			}

			bool shouldShowList = Application.isPlaying && enabled;

			if (!shouldShowList)
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
				if (model.Expanded)
				{
					GUIStyle style = new GUIStyle(GUI.skin.label);
					if (model == currentModel)
					{
						style.fontStyle = FontStyle.Bold;
					}
					GUILayout.Label(model.Name, style);

					foreach (var variant in model.Variants)
					{
						DrawModelLoadButton(variant.Name, variant.ModelFilePath);
					}
				}
				else
				{
					DrawModelLoadButton(model.Name, model.DefaultFilePath);
				}

				if (model.Variants.Count > 1)
				{
					model.Expanded = DrawExpandCollapseButton(model.Expanded, model.Variants.Count);
				}
			}
		}

		private bool DrawExpandCollapseButton(bool expanded, int count)
		{
			string expandCollapseText = expanded ? "-" : "+";
			string buttonText = $"{expandCollapseText} ({count})";
			var buttonPressed = GUILayout.Button(buttonText, GUILayout.Width(40));

			if (buttonPressed)
			{
				return !expanded;
			}
			else
			{
				return expanded;
			}
		}

		private void DrawModelLoadButton(string title, string modelRelativePath)
		{
			var buttonPressed = GUILayout.Button(title);

			if (buttonPressed)
			{
				//currentModel = model;
				LoadModel(modelRelativePath);
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

			var jsonReader = CreateJsonReaderFromStream(loader.LoadedStream);
			jsonReader.Read();
			var listType = SampleModelListParser.DetermineListSource(jsonReader);

			jsonReader = CreateJsonReaderFromStream(loader.LoadedStream);
			jsonReader.Read();


			if (listType == SampleModelListParser.ListType.SampleModels)
			{
				models = SampleModelListParser.ParseSampleModels(jsonReader);
			}
			else
			{
				models = SampleModelListParser.ParseAssetGeneratorModels(jsonReader);
			}
		}

		private JsonReader CreateJsonReaderFromStream(Stream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);

			var streamReader = new StreamReader(stream);

			return new JsonTextReader(streamReader);
		}
	}
}
