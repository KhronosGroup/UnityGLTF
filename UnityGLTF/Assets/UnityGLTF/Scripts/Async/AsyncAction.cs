using System;
using System.Collections;
using System.Threading;

namespace UnityGLTF
{
	/// <summary>
	/// Creates a thread to run multithreaded operations on
	/// </summary>
	public class AsyncAction
	{
#if !WINDOWS_UWP
		private bool _workerThreadRunning = false;
		private Exception _savedException;

		public IEnumerator RunOnWorkerThread(Action action)
		{
			_workerThreadRunning = true;

			ThreadPool.QueueUserWorkItem((_) =>
			{
				try
				{
					action();
				}
				catch (Exception e)
				{
					_savedException = e;
				}

				_workerThreadRunning = false;
			});

			yield return Wait();

			if (_savedException != null)
			{
				throw _savedException;
			}
		}

		private IEnumerator Wait()
		{
			while (_workerThreadRunning)
			{
				yield return null;
			}
		}
#endif
	}
}
