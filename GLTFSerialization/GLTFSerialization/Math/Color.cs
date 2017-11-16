namespace GLTF.Math
{
	public struct Color
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

		public static bool operator ==(Color left, Color right)
		{
			return left.R == right.R && left.B == right.B && left.G == right.G && left.A == right.A;
		}

		public static bool operator !=(Color left, Color right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Color))
			{
				base.Equals(obj);
			}

			return this == (Color)obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
