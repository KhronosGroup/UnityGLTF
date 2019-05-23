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

			if (serializedObject.targetObject is GLTFComponent gltfComponent
				&& gltfComponent.Animations != null)
			{
				EditorGUILayout.LabelField("Animations:");
				using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
				{
					scrollPosition = scrollView.scrollPosition;
					//GUILayout.
					foreach (var animation in gltfComponent.Animations)
					{
						DrawAnimation(animation);
					}
				}
			}
		}

		private void DrawAnimation(Animation animation)
		{
			using (var horizontal = new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label(animation.name);

				GUILayout.Label(animation.GetClipCount().ToString());

				string playPause = animation.isPlaying ? "pause" : "play";

				var buttonPressed = GUILayout.Button(playPause);

				if (buttonPressed)
				{
					if (animation.isPlaying)
					{
						//animation.Stop();
						SetClipsPlaying(animation, false);
					}
					else
					{
						//animation.Play();
						SetClipsPlaying(animation, true);
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
