using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback.Extensions
{
    public static partial class ExtensionMethods
    {
        public static bool IsNullOrEmpty<T>(this T[] arr)
        {
            if (arr == null)
                return true;

            if (arr.Length <= 0)
                return true;

            return false;
        }

        public static bool IsNullOrEmpty<T>(this List<T> arr)
        {
            if (arr == null)
                return true;

            if (arr.Count <= 0)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this ReadOnlySpan<char> span, string str)
        {
            return span.SequenceEqual(str.AsSpan());
        }

        public static void Shuffle<T>(this System.Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }
}