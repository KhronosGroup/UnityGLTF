#if WINDOWS_UWP
using System.Collections;
using System.Threading.Tasks;
#endif

namespace UnityGLTF
{
#if WINDOWS_UWP
	public static class TaskExtensions
	{
		public static IEnumerator AsCoroutine(this Task task)
		{
			while (!task.IsCompleted)
			{
				yield return null;
			}
		}
	}
#endif
}
