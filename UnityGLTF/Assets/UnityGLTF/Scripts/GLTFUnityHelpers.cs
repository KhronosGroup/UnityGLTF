using UnityEngine;

namespace UnityGLTF
{
    /// <summary>
    /// Contains methods which help flip a model's coordinate system.
    /// These methods can be used to change a model's coordinate system from
    /// the glTF standard to Unity's coordinate system. Additionally,
    /// methods for flipping the faces of a model and for flipping a
    /// TexCoord's Y axis to match Unity's definition are present.
    /// </summary>
    /// <remarks>
    /// Methods for both UnityEngine vectors and GLTF.Math vectors are provided.
    /// </remarks>
    public static class GLTFUnityHelpers
    {
        /// <summary>
        /// Flips the Y axis of a TexCoord to convert between glTF and Unity's TexCoord specification.
        /// </summary>
        /// <param name="vector2">The TexCoord to be converted.</param>
        /// <returns>The converted TexCoord.</returns>
        public static Vector2 FlipTexCoordY(Vector2 vector2)
        {
            vector2.y = 1 - vector2.y;
            return vector2;
        }

        /// <summary>
        /// Flips the Y axis of a TexCoord to convert between glTF and Unity's TexCoord specification.
        /// </summary>
        /// <param name="vector2">The TexCoord to be converted.</param>
        /// <returns>The converted TexCoord.</returns>
        public static GLTF.Math.Vector2 FlipTexCoordY(GLTF.Math.Vector2 vector2)
        {
            vector2.Y = 1 - vector2.Y;
            return vector2;
        }

        /// <summary>
        /// Flips the Y axis of all TexCoords in an array to convert between glTF and Unity's TexCoord specification.
        /// </summary>
        /// <param name="arr">The array of TexCoords to be converted.</param>
        /// <returns>The array of converted TexCoords.</returns>
        public static Vector2[] FlipTexCoordArrayY(Vector2[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipTexCoordY(arr[i]);
            }
            return arr;
        }

        /// <summary>
        /// Flips the Y axis of all TexCoords in an array to convert between glTF and Unity's TexCoord specification.
        /// </summary>
        /// <param name="arr">The array of TexCoords to be converted.</param>
        /// <returns>The array of converted TexCoords.</returns>
        public static GLTF.Math.Vector2[] FlipTexCoordArrayY(GLTF.Math.Vector2[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipTexCoordY(arr[i]);
            }
            return arr;
        }

        /// <summary>
        /// Inverts the Z value of a Vector3 to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="vector3">The Vector3 to be converted.</param>
        /// <returns>The converted Vector3.</returns>
        public static Vector3 FlipVectorHandedness(Vector3 vector3)
        {
            vector3.z = -vector3.z;
            return vector3;
        }

        /// <summary>
        /// Inverts the Z value of a Vector3 to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="vector3">The Vector3 to be converted.</param>
        /// <returns>The converted Vector3.</returns>
        public static GLTF.Math.Vector3 FlipVectorHandedness(GLTF.Math.Vector3 vector3)
        {
            vector3.Z = -vector3.Z;
            return vector3;
        }

        /// <summary>
        /// Inverts the Z value of all Vector3s in an array to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="arr">The array of Vector3s to be converted.</param>
        /// <returns>The array of converted Vector3s.</returns>
        public static Vector3[] FlipVectorArrayHandedness(Vector3[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        /// <summary>
        /// Inverts the Z value of all Vector3s in an array to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="arr">The array of Vector3s to be converted.</param>
        /// <returns>The array of converted Vector3s.</returns>
        public static GLTF.Math.Vector3[] FlipVectorArrayHandedness(GLTF.Math.Vector3[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        /// <summary>
        /// Inverts the Z and W values of a Vector4 to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="vector4">The Vector4 to be converted.</param>
        /// <returns>The converted Vector4.</returns>
        public static Vector4 FlipVectorHandedness(Vector4 vector4)
        {
            vector4.z = -vector4.z;
            vector4.w = -vector4.w;
            return vector4;
        }

        /// <summary>
        /// Inverts the Z and W values of a Vector4 to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="vector4">The Vector4 to be converted.</param>
        /// <returns>The converted Vector4.</returns>
        public static GLTF.Math.Vector4 FlipVectorHandedness(GLTF.Math.Vector4 vector4)
        {
            vector4.Z = -vector4.Z;
            vector4.W = -vector4.W;
            return vector4;
        }

        /// <summary>
        /// Inverts the Z and W values of all Vector4s in an array to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="arr">The array of Vector4s to be converted.</param>
        /// <returns>The array of converted Vector4s.</returns>
        public static Vector4[] FlipVectorArrayHandedness(Vector4[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        /// <summary>
        /// Inverts the Z and W values of all Vector4s in an array to convert between glTF and Unity's coordinate systems.
        /// </summary>
        /// <param name="arr">The array of Vector4s to be converted.</param>
        /// <returns>The array of converted Vector4s.</returns>
        public static GLTF.Math.Vector4[] FlipVectorArrayHandedness(GLTF.Math.Vector4[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        /// <summary>
        /// Flips the faces of a model by changing the order of the index array.
        /// </summary>
        /// <param name="array">An array of uints, representing the indices to be rotated.</param>
        /// <returns>The flipped array of indices.</returns>
        public static uint[] FlipFaces(uint[] array)
        {
            var returnArray = new uint[array.Length];

            for (int i = 0; i < array.Length; i += 3)
            {
                returnArray[i] = array[i + 2];
                returnArray[i + 1] = array[i + 1];
                returnArray[i + 2] = array[i];
            }

            return returnArray;
        }

        /// <summary>
        /// Flips the faces of a model by changing the order of the index array.
        /// </summary>
        /// <param name="arr">An array of ints, representing the indices to be rotated.</param>
        /// <returns>The flipped array of indices.</returns>
        public static int[] FlipFaces(int[] array)
        {
            var returnArray = new int[array.Length];

            for (int i = 0; i < array.Length; i += 3)
            {
                returnArray[i] = array[i + 2];
                returnArray[i + 1] = array[i + 1];
                returnArray[i + 2] = array[i];
            }

            return returnArray;
        }
    }
}
