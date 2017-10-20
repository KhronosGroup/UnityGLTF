using System;

namespace GLTF.Math
{
	public struct Vector4 : IEquatable<Vector4>
	{
		public Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float W { get; set; }

	    public bool Equals(Vector4 other)
	    {
	        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
	    }

	    public override bool Equals(object obj)
	    {
	        if (ReferenceEquals(null, obj)) return false;
	        return obj is Vector4 && Equals((Vector4) obj);
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

	    public static bool operator ==(Vector4 left, Vector4 right)
	    {
	        return left.Equals(right);
	    }

	    public static bool operator !=(Vector4 left, Vector4 right)
	    {
	        return !left.Equals(right);
        }
    }
}
