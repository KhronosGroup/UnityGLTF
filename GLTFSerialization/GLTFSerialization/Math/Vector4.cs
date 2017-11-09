namespace GLTF.Math
{
	public struct Vector4
	{
		public Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public Vector4(Vector4 other)
		{
			X = other.X;
			Y = other.Y;
			Z = other.Z;
			W = other.W;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Vector4))
			{
				base.Equals(obj);
			}

			return this == (Vector4)obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float W { get; set; }

		public static bool operator ==(Vector4 left, Vector4 right)
		{
			return left.X == right.X && left.Y == right.Y && left.Z == right.Z && left.W == right.W;
		}

		public static bool operator !=(Vector4 left, Vector4 right)
		{
			return !(left == right);
		}
	}
}
