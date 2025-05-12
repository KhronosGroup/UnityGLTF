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
            var unitsA = a.fromSocket.AsSpan();
            var unitsB = b.fromSocket.AsSpan();

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

            return 0;
        }
    }
}