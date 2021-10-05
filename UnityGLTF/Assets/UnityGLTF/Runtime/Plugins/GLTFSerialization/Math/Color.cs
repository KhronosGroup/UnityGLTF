using System;

namespace GLTF.Math
{
	public struct Color : IEquatable<Color>
	{
		public static Color Black { get { return new Color(0f, 0f, 0f, 1f); } }
		public static Color White { get { return new Color(1f, 1f, 1f, 1f); } }

		public float R { get; set; }
		public float G { get; set; }
		public float B { get; set; }
		public float A { get; set; }

		public Color(float r, float g, float b, float a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public Color(Color other)
		{
			R = other.R;
			G = other.G;
			B = other.B;
			A = other.A;
		}

		public bool Equals(Color other)
		{
			return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Color && Equals((Color) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = R.GetHashCode();
				hashCode = (hashCode * 397) ^ G.GetHashCode();
				hashCode = (hashCode * 397) ^ B.GetHashCode();
				hashCode = (hashCode * 397) ^ A.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(Color left, Color right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Color left, Color right)
		{
			return !left.Equals(right);
		}
	}
}
