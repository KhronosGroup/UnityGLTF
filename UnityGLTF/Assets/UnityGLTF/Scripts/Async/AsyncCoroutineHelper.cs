
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Pisa.DataServices.Profiling;
using UnityEngine;

namespace UnityGLTF
{
	public class AsyncCoroutineHelper : MonoBehaviour
	{
		private Queue<CoroutineInfo> actions = new Queue<CoroutineInfo>();
		
		public Task RunAsTask(IEnumerator coroutine, string name)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			lock (actions)
			{
				actions.Enqueue(
					new CoroutineInfo
					{
						Coroutine = coroutine,
						Tcs = tcs,
						Name = name
					}
				);
			}

			return tcs.Task;
		}

		private IEnumerator CallMethodOnMainThread(CoroutineInfo coroutineInfo)
		{
            Profiler.AddEvent("Running coroutine: " + coroutineInfo.Name);
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
			public string Name;
		}
	}
}
