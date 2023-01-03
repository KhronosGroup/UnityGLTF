using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Cache
{
	internal static class ExportCache
	{
		public static string CacheDirectory
		{
			get
			{
				var tempDirectory = Path.Combine(Application.temporaryCachePath, "UnityGLTF");
				return tempDirectory;
			}
		}

		public static void Clear()
		{
			var dir = CacheDirectory;
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, true);
			}
		}

		public static bool TryGetBytes(Object asset, string seed, out byte[] bytes)
		{
#if UNITY_EDITOR
			var path = CacheDirectory + "/" + GlobalObjectId.GetGlobalObjectIdSlow(asset) + seed;
			if (File.Exists(path))
			{
				bytes = File.ReadAllBytes(path);
				return true;
			}
#endif

			bytes = null;
			return false;
		}

		public static void AddBytes(Object asset, string seed, byte[] bytes)
		{
#if UNITY_EDITOR
			var dir = CacheDirectory;
			Directory.CreateDirectory(dir);
			var path = dir + "/" + GlobalObjectId.GetGlobalObjectIdSlow(asset) + seed;
			Debug.Log("Writing to cache: " + path);
			File.WriteAllBytes(path, bytes);
#endif
		}
	}
}
