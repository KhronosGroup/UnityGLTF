using System.IO;
using System;
using UnityEngine;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public static partial class Helpers
    {
        public static int CompareTo(this Flow a, Flow b)
        {
            return CompareSocketIds(a.fromSocket, b.fromSocket);
        }

        /// <summary>
        /// Socket order from the spec: lexicographic over UTF-16 code units, shorter id first on a shared prefix.
        /// </summary>
        public static int CompareSocketIds(string a, string b)
        {
            var unitsA = a.AsSpan();
            var unitsB = b.AsSpan();

            var lengthA = unitsA.Length;
            var lengthB = unitsB.Length;

            var minLength = math.min(lengthA, lengthB);

            for (int i = 0; i < minLength; i++)
            {
                if (unitsA[i] < unitsB[i])
                    return -1;

                if (unitsA[i] > unitsB[i])
                    return 1;
            }

            return lengthA.CompareTo(lengthB);
        }
    }
}