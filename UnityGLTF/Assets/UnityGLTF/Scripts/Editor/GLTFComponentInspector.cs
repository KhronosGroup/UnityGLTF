using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	[CustomEditor(typeof(GLTFComponent))]
	public class GLTFComponentInspector : Editor
	{
		private Vector2 scrollPosition = Vector2.zero;

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();

			var gltfComponent = serializedObject.targetObject as GLTFComponent;

			if (gltfComponent != null &&
				gltfComponent.Animations != null)
			{
				EditorGUILayout.LabelField("Animations:");
				using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
				{
					scrollPosition = scrollView.scrollPosition;
					foreach (var animation in gltfComponent.Animations)
					{
						DrawAnimation(animation);
					}
				}
			}
		}

		private void DrawAnimation(Animation animation)
		{
			GUILayout.Label(animation.name);

			foreach (AnimationState animationState in animation)
			{
				using (var horizontal = new EditorGUILayout.HorizontalScope())
				{
					var clip = animationState.clip;
					var clipName = clip.name;
					GUILayout.Label(clipName);

					var isPlaying = animation.IsPlaying(clipName);
					string playPause = isPlaying ? "pause" : "play";

					var buttonPressed = GUILayout.Button(playPause);

					if (buttonPressed)
					{
						if (isPlaying)
						{
							animation.Stop(clipName);
						}
						else
						{
							animation.Play(clipName);
						}
					}
				}
			}
		}

		private void SetClipsPlaying(Animation animation, bool play)
		{
			foreach (AnimationState animationState in animation)
			{
				if (play)
				{
					animation.Play(animationState.clip.name);
				}
				else
				{
					animation.Stop(animationState.clip.name);
				}
			}
		}
	}
}
