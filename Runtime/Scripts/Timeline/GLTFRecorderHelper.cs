using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Timeline
{
	[ExecuteInEditMode]
	internal class GLTFRecorderHelper : MonoBehaviour
    {
		public static void Add(Action callback)
	    {
		    GetInstance().queuedActions.Add(callback);
	    }

	    private List<Action> queuedActions = new List<Action>();

        private static GLTFRecorderHelper instance;

        static GLTFRecorderHelper GetInstance()
        {
	        if (instance != null) return instance;
#if UNITY_2023_1_OR_NEWER
	        instance = FindFirstObjectByType<GLTFRecorderHelper>();
 #else
	        instance = FindObjectOfType<GLTFRecorderHelper>();
#endif
	        if (instance != null) return instance;

	        var go = new GameObject
            {
	            name = "GLTF Recorder Helper",
	            hideFlags = HideFlags.HideAndDontSave,
            };
            instance = go.AddComponent<GLTFRecorderHelper>();

            return instance;
        }

        private IEnumerator WaitForEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            foreach (var callback in queuedActions)
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            queuedActions.Clear();
        }

        private void LateUpdate()
        {
            StartCoroutine(WaitForEndOfFrame());
        }
    }
}
