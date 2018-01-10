using UnityEngine;

namespace UnityGLTF
{
    public static class GLTFUnityHelpers
    {
        public static Vector2 FlipTexCoordY(Vector2 vector2)
        {
            vector2.y = 1 - vector2.y;
            return vector2;
        }

        public static GLTF.Math.Vector2 FlipTexCoordY(GLTF.Math.Vector2 vector2)
        {
            vector2.Y = 1 - vector2.Y;
            return vector2;
        }

        public static Vector2[] FlipTexCoordArrayY(Vector2[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipTexCoordY(arr[i]);
            }
            return arr;
        }

        public static GLTF.Math.Vector2[] FlipTexCoordArrayY(GLTF.Math.Vector2[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipTexCoordY(arr[i]);
            }
            return arr;
        }

        public static Vector3 FlipVectorHandedness(Vector3 vector3)
        {
            vector3.z = -vector3.z;
            return vector3;
        }

        public static GLTF.Math.Vector3 FlipVectorHandedness(GLTF.Math.Vector3 vector3)
        {
            vector3.Z = -vector3.Z;
            return vector3;
        }

        public static Vector3[] FlipVectorArrayHandedness(Vector3[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        public static GLTF.Math.Vector3[] FlipVectorArrayHandedness(GLTF.Math.Vector3[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        public static Vector4 FlipVectorHandedness(Vector4 vector4)
        {
            vector4.z = -vector4.z;
            vector4.w = -vector4.w;
            return vector4;
        }

        public static GLTF.Math.Vector4 FlipVectorHandedness(GLTF.Math.Vector4 vector4)
        {
            vector4.Z = -vector4.Z;
            vector4.W = -vector4.W;
            return vector4;
        }

        public static Vector4[] FlipVectorArrayHandedness(Vector4[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

        public static GLTF.Math.Vector4[] FlipVectorArrayHandedness(GLTF.Math.Vector4[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = FlipVectorHandedness(arr[i]);
            }
            return arr;
        }

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
