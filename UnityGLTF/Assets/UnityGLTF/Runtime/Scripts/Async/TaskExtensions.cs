#if UNITY_2017_1_OR_NEWER
using System.Collections;
using System.Threading.Tasks;

namespace UnityGLTF
{
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
}
#endif