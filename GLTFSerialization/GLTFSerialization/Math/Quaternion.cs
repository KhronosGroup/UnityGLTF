namespace GLTF.Math
{
	public struct Quaternion
	{
		public static readonly Quaternion Identity = new Quaternion(0f, 0f, 0f, 1f);

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

		public override bool Equals(object obj)
		{
			if(!(obj is Quaternion))
			{
				base.Equals(obj);
			}

			return this == (Quaternion)obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float W { get; set; }

		public static bool operator ==(Quaternion left, Quaternion right)
		{
			return left.X == right.X && left.Y == right.Y && left.Z == right.Z && left.W == right.W;
		}

		public static bool operator !=(Quaternion left, Quaternion right)
		{
			return !(left == right);
		}
	}
}