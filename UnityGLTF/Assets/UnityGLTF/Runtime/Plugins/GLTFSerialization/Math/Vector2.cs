using System;

namespace GLTF.Math
{
	public struct Vector2 : IEquatable<Vector2>
	{
		public float X { get; set; }
		public float Y { get; set; }
	
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

		public bool Equals(Vector2 other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Vector2 && Equals((Vector2) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (X.GetHashCode() * 397) ^ Y.GetHashCode();
			}
		}

		public static bool operator ==(Vector2 left, Vector2 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Vector2 left, Vector2 right)
		{
			return !left.Equals(right);
		}
	}
}
