using System;
using System.IO;

namespace UnityGLTF
{
	public static class LongPath
	{
#if UNITY_EDITOR_WIN
		private const string Prefix = @"\\?\";
		private const string UncPrefix = @"\\?\UNC\";
#endif

		/// <summary>
		/// Returns an absolute path prefixed with \\?\ so System.IO calls are not limited to MAX_PATH (260 chars) on Windows.
		/// The Unity editor is not longPathAware, so this is required even when LongPathsEnabled is set in the registry.
		/// Only pass the result to System.IO (File, Directory, FileStream...) - not to Unity APIs (AssetDatabase), URIs or UnityWebRequest.
		/// Only active in the Windows editor - returns the path unchanged on other platforms and in player builds.
		/// </summary>
		public static string ToLongPath(string path)
		{
#if UNITY_EDITOR_WIN
			if (string.IsNullOrEmpty(path)) return path;
			if (path.StartsWith(Prefix) || path.StartsWith(@"\\.\")) return path;
			string fullPath;
			try
			{
				fullPath = Path.GetFullPath(path).Replace('/', '\\');
			}
			catch (Exception)
			{
				// not a valid file path (e.g. invalid characters) - let the IO call handle it as before
				return path;
			}
			if (fullPath.StartsWith(@"\\")) return UncPrefix + fullPath.Substring(2);
			return Prefix + fullPath;
#else
			return path;
#endif
		}
	}
}
