namespace GLTFJsonSerialization.Math
{
    public class Color
    {
        public static Color Black { get { return new Color(0f, 0f, 0f, 1f); } }
        public static Color White { get { return new Color(1f, 1f, 1f, 1f); } }

        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public Color()
        {
        }

        public Color(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
