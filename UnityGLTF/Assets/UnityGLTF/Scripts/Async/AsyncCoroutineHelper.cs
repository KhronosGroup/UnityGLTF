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
		private Queue<IEnumerator> actions = new Queue<IEnumerator>();
		
		public Task<TResult> RunOnMainThread<T, TResult>(Func<T, TResult> methodToRun, T param1)
		{
			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
			RunOnMainThread(CallMethodOnMainThread<T, TResult>(methodToRun, param1, tcs));
			return tcs.Task;
		}

		public void RunOnMainThread(IEnumerator coroutine)
		{
			lock (actions)
			{
				actions.Enqueue(coroutine);
			}
		}

		private IEnumerator CallMethodOnMainThread<T, TResult>(Func<T, TResult> methodToRun, T param1, TaskCompletionSource<TResult> tcs)
		{
			TResult callbackResult = methodToRun(param1);
			yield return null;
			tcs.SetResult(callbackResult);
		}

		private void Update()
		{
			lock(actions)
			{
				while(actions.Count > 0)
				{
					IEnumerator coroutine = actions.Dequeue();
					StartCoroutine(coroutine);
				}
			}
		}
#endif
	}
}
