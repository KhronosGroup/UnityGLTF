using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Cache
{
	internal static class ExportCache
	{
		public const int DefaultCacheSize = 1024;
#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void Init()
		{
			// Keep some files in cache that were last exported
			EditorApplication.quitting += () => Shrink(DefaultCacheSize);
		}

		public static void OpenCacheDirectory()
		{
			var dir = CacheDirectory;
			if(Directory.Exists(dir)) EditorUtility.RevealInFinder(dir);
			else Debug.LogWarning($"Cache directory does not exist: {dir}");
		}
#endif

		public static string CacheDirectory
		{
			get
			{
				var tempDirectory = Path.Combine(Application.temporaryCachePath, "UnityGLTF");
				return tempDirectory;
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
			// Debug.Log($"Writing {bytes.Length} bytes to cache: {path}");
			File.WriteAllBytes(path, bytes);
#endif
		}

		public static void Clear()
		{
			var dir = CacheDirectory;
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, true);
			}
		}

		public static void Shrink(int maxCacheSizeInMB)
		{
			if (maxCacheSizeInMB <= 0)
			{
				Clear();
				return;
			}

			var maxCacheSize = maxCacheSizeInMB * 1024 * 1024;
			var files = new List<FileInfo>();
			var currentSize = CalculateCacheSize(files);
			if(currentSize <= maxCacheSizeInMB)
				return;
			var filesSortedByLastAccess = files.OrderBy(f => f.LastAccessTimeUtc).ToList();
			foreach (var file in filesSortedByLastAccess)
			{
				file.Delete();
				currentSize -= (int)file.Length;
				if (currentSize <= maxCacheSize)
					break;
			}
		}

		public static long CalculateCacheSize(ICollection<FileInfo> files = null)
		{
			var dir = CacheDirectory;
			if (!Directory.Exists(dir)) return 0;
			var filePaths = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
			long size = 0;
			foreach (var file in filePaths)
			{
				var info = new FileInfo(file);
				files?.Add(info);
				size += (long) info.Length;
			}
			return size;
		}
	}
}
