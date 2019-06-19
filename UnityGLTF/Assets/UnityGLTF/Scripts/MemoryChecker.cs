using System;
using UnityEngine;

namespace UnityGLTF
{
	public class MemoryChecker
	{
		private bool outOfMemory = false;

		public MemoryChecker()
		{
			Application.lowMemory += Application_lowMemory;

#if WINDOWS_UWP
        MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;
#endif
		}

		public void ThrowIfOutOfMemory()
		{
			//TODO: when should we reset outOfMemory to false?

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
