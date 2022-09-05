using System;

namespace GLTF.Math
{
	public struct Quaternion : IEquatable<Quaternion>
	{
		public static readonly Quaternion Identity = new Quaternion(0f, 0f, 0f, 1f);
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float W { get; set; }
		
		public Quaternion(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public Quaternion(Quaternion other)
		{
			X = other.X;
			Y = other.Y;
			Z = other.Z;
			W = other.W;
		}

		public bool Equals(Quaternion other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Quaternion && Equals((Quaternion) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = X.GetHashCode();
				hashCode = (hashCode * 397) ^ Y.GetHashCode();
				hashCode = (hashCode * 397) ^ Z.GetHashCode();
				hashCode = (hashCode * 397) ^ W.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(Quaternion left, Quaternion right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Quaternion left, Quaternion right)
		{
			return !left.Equals(right);
		}

		/// First quaternion.
		/// Second quaternion.
		/// Result of multiplication.
		public static Quaternion operator *(Quaternion left, Quaternion right)
		{
			float x = left.W * right.X + left.X * right.W + left.Y * right.Z - left.Z * right.Y;
			float y = left.W * right.Y + left.Y * right.W + left.Z * right.X - left.X * right.Z;
			float z = left.W * right.Z + left.Z * right.W + left.X * right.Y - left.Y * right.X;
			float w = left.W * right.W - left.X * right.X - left.Y * right.Y - left.Z * right.Z;
			Quaternion result = new Quaternion(x, y, z, w);
			return result;

		}
	}
}