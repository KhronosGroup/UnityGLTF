using System;
using UnityEngine;
#if WINDOWS_UWP
using Windows.System;
#endif

namespace UnityGLTF
{
	public class MemoryChecker
	{
		private bool outOfMemory = false;

		/// <summary>
		/// Allows polling for app low memory situation.  Listens to Application.lowMemory, which works for iOS and Android.
		/// Also listens to Windows-specific MemoryManager.AppMemoryUsageIncreased.
		/// </summary>
		public MemoryChecker()
		{
			Application.lowMemory += Application_lowMemory;

#if WINDOWS_UWP
			MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;
#endif
		}

		/// <summary>
		/// If the OS has notified the app that it is low on memory since MemoryChecker was constructed, this method will throw an OutOfMemoryException.
		/// Once it throws once, it will continue to throw.  Callers are advised to construct a new MemoryChecker after memory usage has gone down.
		/// </summary>
		public void ThrowIfOutOfMemory()
		{
			if (outOfMemory)
			{
				throw new OutOfMemoryException();
			}
		}

		private void Application_lowMemory()
		{
			outOfMemory = true;
		}

#if WINDOWS_UWP
		private void MemoryManager_AppMemoryUsageIncreased(object sender, object e)
		{
			if (MemoryManager.AppMemoryUsageLevel == AppMemoryUsageLevel.OverLimit)
			{
				outOfMemory = true;
			}
		}
#endif
	}
}
