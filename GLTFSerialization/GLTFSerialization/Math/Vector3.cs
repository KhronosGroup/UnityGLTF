namespace GLTF.Math
{
	public struct Vector3
	{
		public static readonly Vector3 Zero = new Vector3(0f, 0f, 0f);
		public static readonly Vector3 One = new Vector3(1f, 1f, 1f);

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public Vector3(Vector3 other)
		{
			X = other.X;
			Y = other.Y;
			Z = other.Z;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Vector3))
			{
				base.Equals(obj);
			}

			return this == (Vector3)obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }

		public static bool operator ==(Vector3 left, Vector3 right)
		{
			return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
		}

		public static bool operator !=(Vector3 left, Vector3 right)
		{
			return !(left == right);
		}
	}
}
