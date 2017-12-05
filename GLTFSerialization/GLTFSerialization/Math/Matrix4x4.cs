using System;

namespace GLTF.Math
{
	// class is naively implemented
	public class Matrix4x4
	{
		public static readonly Matrix4x4 Identity = new Matrix4x4(
			1f, 0f, 0f, 0f,
			0f, 1f, 0f, 0f,
			0f, 0f, 1f, 0f,
			0f, 0f, 0f, 1f
			);

		/// <summary>
		/// Matrix is column major ordered
		/// </summary>
		public Matrix4x4(float m11, float m21, float m31, float m41, float m12, float m22, float m32, float m42, float m13, float m23, float m33, float m43, float m14, float m24, float m34, float m44)
		{
			M11 = m11;
			M12 = m12;
			M13 = m13;
			M14 = m14;
			M21 = m21;
			M22 = m22;
			M23 = m23;
			M24 = m24;
			M31 = m31;
			M32 = m32;
			M33 = m33;
			M34 = m34;
			M41 = m41;
			M42 = m42;
			M43 = m43;
			M44 = m44;
		}

		public Matrix4x4(Matrix4x4 other)
		{
			Array.Copy(other.mat, 0, mat, 0, 16);
		}

		public Matrix4x4(Vector4 column1, Vector4 column2, Vector4 column3, Vector4 column4)
		{
			M11 = column1.X;
			M21 = column1.Y;
			M31 = column1.Z;
			M41 = column1.W;

			M12 = column2.X;
			M22 = column2.Y;
			M32 = column2.Z;
			M42 = column2.W;

			M13 = column3.X;
			M23 = column3.Y;
			M33 = column3.Z;
			M43 = column3.W;

			M14 = column4.X;
			M24 = column4.Y;
			M34 = column4.Z;
			M44 = column4.W;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Matrix4x4))
			{
				base.Equals(obj);
			}

			return (obj as Matrix4x4) == this;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		private float[] mat = new float[16];

		public float M11 { get { return mat[0]; } set { mat[0] = value; } }
		public float M21 { get { return mat[1]; } set { mat[1] = value; } }
		public float M31 { get { return mat[2]; } set { mat[2] = value; } }
		public float M41 { get { return mat[3]; } set { mat[3] = value; } }
		public float M12 { get { return mat[4]; } set { mat[4] = value; } }
		public float M22 { get { return mat[5]; } set { mat[5] = value; } }
		public float M32 { get { return mat[6]; } set { mat[6] = value; } }
		public float M42 { get { return mat[7]; } set { mat[7] = value; } }
		public float M13 { get { return mat[8]; } set { mat[8] = value; } }
		public float M23 { get { return mat[9]; } set { mat[9] = value; } }
		public float M33 { get { return mat[10]; } set { mat[10] = value; } }
		public float M43 { get { return mat[11]; } set { mat[11] = value; } }
		public float M14 { get { return mat[12]; } set { mat[12] = value; } }
		public float M24 { get { return mat[13]; } set { mat[13] = value; } }
		public float M34 { get { return mat[14]; } set { mat[14] = value; } }
		public float M44 { get { return mat[15]; } set { mat[15] = value; } }

		public static bool operator ==(Matrix4x4 left, Matrix4x4 right)
		{
			return left.M11 == right.M11 && left.M12 == right.M12 && left.M13 == right.M13 && left.M14 == right.M14 &&
				   left.M21 == right.M21 && left.M22 == right.M22 && left.M23 == right.M23 && left.M24 == right.M24 &&
				   left.M31 == right.M31 && left.M32 == right.M32 && left.M33 == right.M33 && left.M34 == right.M34 &&
				   left.M41 == right.M41 && left.M42 == right.M42 && left.M43 == right.M43 && left.M44 == right.M44;
		}

		public static bool operator !=(Matrix4x4 left, Matrix4x4 right)
		{
			return !(left.mat == right.mat);
		}
	}
}
