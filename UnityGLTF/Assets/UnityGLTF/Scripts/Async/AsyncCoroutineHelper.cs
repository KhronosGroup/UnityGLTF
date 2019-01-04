using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace UnityGLTF
{
    public interface IAsyncCoroutineHelper
    {
	   Task RunAsTask(IEnumerator coroutine, string name);
    }

	public class AsyncCoroutineHelper : MonoBehaviour, IAsyncCoroutineHelper
    {
		private Queue<CoroutineInfo> _actions = new Queue<CoroutineInfo>();

		public Task RunAsTask(IEnumerator coroutine, string name)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			lock (_actions)
			{
				_actions.Enqueue(
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
			yield return coroutineInfo.Coroutine;
			coroutineInfo.Tcs.SetResult(true);
		}

	   private void Update()
		{
			CoroutineInfo? coroutineInfo = null;

			lock (_actions)
			{
				if (_actions.Count > 0)
				{
					coroutineInfo = _actions.Dequeue();
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
