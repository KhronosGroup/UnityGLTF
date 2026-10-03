using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>math/rgbToOkLCh: linear BT.709 RGB to OkLCh, hue in radians.</summary>
    public class MathRgbToOkLCh : BehaviourEngineNode
    {
        public MathRgbToOkLCh(BehaviourEngine engine, Node node) : base(engine, node) { }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.R, out float r);
            TryEvaluateValue(ConstStrings.G, out float g);
            TryEvaluateValue(ConstStrings.B, out float b);

            var lch = Convert(r, g, b);

            return id switch
            {
                ConstStrings.L => Variant.FromFloat(lch.x),
                ConstStrings.C => Variant.FromFloat(lch.y),
                ConstStrings.H => Variant.FromFloat(lch.z),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

        public static float3 Convert(float r, float g, float b)
        {
            var l_ = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
            var m_ = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
            var s_ = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

            // Cube root that supports negative values.
            var l3 = Cbrt(l_);
            var m3 = Cbrt(m_);
            var s3 = Cbrt(s_);

            var L = 0.2104542553 * l3 + 0.7936177850 * m3 - 0.0040720468 * s3;
            var A = 1.9779984951 * l3 - 2.4285922050 * m3 + 0.4505937099 * s3;
            var B = 0.0259040371 * l3 + 0.7827717662 * m3 - 0.8086757660 * s3;

            // h = atan2 with a and b swapped, i.e. atan2(b, a).
            return new float3((float)L, (float)Math.Sqrt(A * A + B * B), (float)Math.Atan2(B, A));
        }

        private static double Cbrt(double v) => v < 0 ? -Math.Pow(-v, 1.0 / 3.0) : Math.Pow(v, 1.0 / 3.0);
    }
}
