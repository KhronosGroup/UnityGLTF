using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>math/rgbFromOkLCh: OkLCh (hue in radians) to linear BT.709 RGB.</summary>
    public class MathRgbFromOkLCh : BehaviourEngineNode
    {
        public MathRgbFromOkLCh(BehaviourEngine engine, Node node) : base(engine, node) { }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.L, out float l);
            TryEvaluateValue(ConstStrings.C, out float c);
            TryEvaluateValue(ConstStrings.H, out float h);

            var rgb = Convert(l, c, h);

            return id switch
            {
                ConstStrings.R => Variant.FromFloat(rgb.x),
                ConstStrings.G => Variant.FromFloat(rgb.y),
                ConstStrings.B => Variant.FromFloat(rgb.z),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

        public static float3 Convert(float l, float c, float h)
        {
            double L = l, a = c * Math.Cos(h), b = c * Math.Sin(h);

            var l_ = L + 0.3963377774 * a + 0.2158037573 * b;
            var m_ = L - 0.1055613458 * a - 0.0638541728 * b;
            var s_ = L - 0.0894841775 * a - 1.2914855480 * b;

            l_ = l_ * l_ * l_;
            m_ = m_ * m_ * m_;
            s_ = s_ * s_ * s_;

            return new float3(
                (float)(4.0767416621 * l_ - 3.3077115913 * m_ + 0.2309699292 * s_),
                (float)(-1.2684380046 * l_ + 2.6097574011 * m_ - 0.3413193965 * s_),
                (float)(-0.0041960863 * l_ - 0.7034186147 * m_ + 1.7076147010 * s_));
        }
    }
}
