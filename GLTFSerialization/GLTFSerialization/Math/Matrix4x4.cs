using System;

namespace GLTF.Math
{
	// class is naively implemented
	public class Matrix4x4 : IEquatable<Matrix4x4>
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

		public bool Equals(Matrix4x4 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 &&
				   M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24 &&
				   M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34 &&
				   M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Matrix4x4) obj);
		}

		public override int GetHashCode()
		{
			return (mat != null ? mat.GetHashCode() : 0);
		}

		public void SetValue(int index, float value)
		{
			if(index > mat.Length)
			{
				throw new IndexOutOfRangeException("Index " + index + " is out of range for a 4x4 matrix.");				
			}

			mat[index] = value;
		}

		public void SetValue(int row, int column, float value)
		{
			switch(row)
			{
				case 0:
					switch (column)
					{
						case 0:
							M11 = value;
							break;
						case 1:
							M12 = value;
							break;
						case 2:
							M13 = value;
							break;
						case 3:
							M14 = value;
							break;
						default:
							throw new IndexOutOfRangeException("Column " + column + " is out of range for a 4x4 matrix.");
					}

					break;
				case 1:
					switch (column)
					{
						case 0:
							M21 = value;
							break;
						case 1:
							M22 = value;
							break;
						case 2:
							M23 = value;
							break;
						case 3:
							M24 = value;
							break;
						default:
							throw new IndexOutOfRangeException("Column " + column + " is out of range for a 4x4 matrix.");
					}

					break;
				case 2:
					switch (column)
					{
						case 0:
							M31 = value;
							break;
						case 1:
							M32 = value;
							break;
						case 2:
							M33 = value;
							break;
						case 3:
							M34 = value;
							break;
						default:
							throw new IndexOutOfRangeException("Column " + column + " is out of range for a 4x4 matrix.");
					}

					break;
				case 3:
					switch (column)
					{
						case 0:
							M41 = value;
							break;
						case 1:
							M42 = value;
							break;
						case 2:
							M43 = value;
							break;
						case 3:
							M44 = value;
							break;
						default:
							throw new IndexOutOfRangeException("Column " + column + " is out of range for a 4x4 matrix.");
					}

					break;
				default:
					throw new IndexOutOfRangeException("Row " + row + " is out of range for a 4x4 matrix.");
			}
		}
	}
}
