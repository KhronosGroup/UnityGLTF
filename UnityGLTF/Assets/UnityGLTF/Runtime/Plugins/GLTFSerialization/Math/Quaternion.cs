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
	}
}