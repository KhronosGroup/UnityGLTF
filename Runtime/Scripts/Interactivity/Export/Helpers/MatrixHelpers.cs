using UnityEngine;

namespace UnityGLTF.Interactivity.Export
{
    public static class MatrixHelpers
    {
        public static readonly string[] MatrixMemberIndex = new string[]
        {
            "m00", "m01", "m02", "m03", "m10", "m11", "m12", "m13",
            "m20", "m21", "m22", "m23", "m30", "m31", "m32", "m33"
        };
        
        /// <summary>
        /// Get the converted GLTF index for a given matrix element. 
        /// </summary>
        public static float GltfGetElement(Matrix4x4 m, int index)
        {
            switch (index)
            {
                case 0: return m.m00;
                case 1: return m.m01;
                case 2: return m.m02;
                case 3: return m.m03;
                case 4: return m.m10;
                case 5: return m.m11;
                case 6: return m.m12;
                case 7: return m.m13;
                case 8: return m.m20;
                case 9: return m.m21;
                case 10: return m.m22;
                case 11: return m.m23;
                case 12: return m.m30;
                case 13: return m.m31;
                case 14: return m.m32;
                case 15: return m.m33;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 15.");
            }
        }
        
        public static void GltfSetElement(ref Matrix4x4 m, int index, float value)
        {
            switch (index)
            {
                case 0: m.m00 = value; break;
                case 1: m.m01 = value; break;
                case 2: m.m02 = value; break;
                case 3: m.m03 = value; break;
                case 4: m.m10 = value; break;
                case 5: m.m11 = value; break;
                case 6: m.m12 = value; break;
                case 7: m.m13 = value; break;
                case 8: m.m20 = value; break;
                case 9: m.m21 = value; break;
                case 10: m.m22 = value; break;
                case 11: m.m23 = value; break;
                case 12: m.m30 = value; break;
                case 13: m.m31 = value; break;
                case 14: m.m32 = value; break;
                case 15: m.m33 = value; break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 15.");
            }
        }
        
    }
}