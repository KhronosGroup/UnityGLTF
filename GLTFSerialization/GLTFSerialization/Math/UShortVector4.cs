using System;

namespace GLTF.Math
{
	public struct UShortVector4 : IEquatable<UShortVector4>
	{
		public static readonly UShortVector4 Zero = new UShortVector4(0, 0, 0, 0);
		public static readonly UShortVector4 One = new UShortVector4(1, 1, 1, 1);

		public ushort X { get; set; }
		public ushort Y { get; set; }
		public ushort Z { get; set; }
		public ushort W { get; set; }

		public UShortVector4(ushort x, ushort y, ushort z, ushort w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}


		public UShortVector4(UShortVector4 other)
		{
			X = other.X;
			Y = other.Y;
			Z = other.Z;
			W = other.W;
		}

		public bool Equals(UShortVector4 other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is UShortVector4 && Equals((UShortVector4)obj);
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

		public static bool operator ==(UShortVector4 left, UShortVector4 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(UShortVector4 left, UShortVector4 right)
		{
			return !left.Equals(right);
		}
	}
}
