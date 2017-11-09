namespace GLTF.Math
{
	public struct Vector2
	{
		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public Vector2(Vector2 other)
		{
			X = other.X;
			Y = other.Y;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Vector2))
			{
				base.Equals(obj);
			}

			return this == (Vector2)obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public float X { get; set; }
		public float Y { get; set; }

		public static bool operator ==(Vector2 left, Vector2 right)
		{
			return left.X == right.X && left.Y == right.Y;
		}

		public static bool operator !=(Vector2 left, Vector2 right)
		{
			return !(left == right);
		}
	}
}
