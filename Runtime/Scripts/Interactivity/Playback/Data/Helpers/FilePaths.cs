using System.IO;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public static partial class Helpers
    {
        public static (string directory, string fileName) GetFilePath(string filePath)
        {
            const int MAX_PATH_LENGTH_LINUX = 4096;

            var sb = new ValueStringBuilder(MAX_PATH_LENGTH_LINUX);

            try
            {
                sb.Append(Application.streamingAssetsPath);

                if (!(filePath.StartsWith(Path.DirectorySeparatorChar) || filePath.StartsWith(Path.AltDirectorySeparatorChar)))
                    sb.Append('/');

                sb.Append(filePath);

                var lastIndex1 = sb.LastIndexOf(Path.DirectorySeparatorChar);
                var lastIndex2 = sb.LastIndexOf(Path.AltDirectorySeparatorChar);

                var lastIndex = Mathf.Max(lastIndex1, lastIndex2);

                var directory = sb.ToString(0, lastIndex);
                var fileName = sb.ToString(lastIndex + 1, sb.length);

                return (directory, fileName);
            }
            finally
            {
                sb.Dispose();
            }
        }
    }
}