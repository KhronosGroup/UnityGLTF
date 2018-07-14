using System;

namespace GLTF.Math
{
	public struct ByteVector4 : IEquatable<ByteVector4>
	{
		public static readonly ByteVector4 Zero = new ByteVector4(0, 0, 0, 0);
		public static readonly ByteVector4 One = new ByteVector4(1, 1, 1, 1);

		public byte X { get; set; }
		public byte Y { get; set; }
		public byte Z { get; set; }
		public byte W { get; set; }

		public ByteVector4(byte x, byte y, byte z, byte w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}


		public ByteVector4(ByteVector4 other)
		{
			X = other.X;
			Y = other.Y;
			Z = other.Z;
			W = other.W;
		}

		public bool Equals(ByteVector4 other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is ByteVector4 && Equals((ByteVector4)obj);
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

		public static bool operator ==(ByteVector4 left, ByteVector4 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ByteVector4 left, ByteVector4 right)
		{
			return !left.Equals(right);
		}
	}
}
