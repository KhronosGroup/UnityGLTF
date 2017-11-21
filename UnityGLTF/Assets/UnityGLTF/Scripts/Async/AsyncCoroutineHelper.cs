
using System;
using System.Collections;
using System.Collections.Generic;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif
using UnityEngine;

namespace UnityGLTF
{
	public class AsyncCoroutineHelper : MonoBehaviour
	{
#if WINDOWS_UWP
		private Queue<CoroutineInfo> actions = new Queue<CoroutineInfo>();
		
		public Task RunAsTask(IEnumerator coroutine)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			lock (actions)
			{
				actions.Enqueue(
					new CoroutineInfo
					{
						Coroutine = coroutine,
						Tcs = tcs
					}
				);
			}

			return tcs.Task;
		}

		private IEnumerator CallMethodOnMainThread(CoroutineInfo coroutineInfo)
		{
			yield return coroutineInfo.Coroutine;
			coroutineInfo.Tcs.SetResult(true);
		}

		private void Update()
		{
			CoroutineInfo? coroutineInfo = null;
			
			lock (actions)
			{
				if (actions.Count > 0)
				{
					coroutineInfo = actions.Dequeue();
				}
			}

			if (coroutineInfo != null)
			{
				StartCoroutine(CallMethodOnMainThread(coroutineInfo.Value));
			}
		}

		private struct CoroutineInfo
		{
			public IEnumerator Coroutine;
			public TaskCompletionSource<bool> Tcs;
		}
#endif
	}
}
