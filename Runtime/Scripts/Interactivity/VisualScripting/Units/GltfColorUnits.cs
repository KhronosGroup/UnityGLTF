using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// KHR_interactivity math/rgbToOkLCh: converts a linear RGB color (BT.709) to OkLCh.
    /// Hue is in radians. Use Color.linear first for colors in gamma (sRGB) space.
    /// </summary>
    [UnitCategory("Math/Color")]
    [UnitTitle("RGB To OkLCh (glTF)")]
    public class GltfRgbToOkLCh : Unit
    {
        [DoNotSerialize] public ValueInput color { get; private set; }
        [DoNotSerialize] public ValueOutput l { get; private set; }
        [DoNotSerialize] public ValueOutput c { get; private set; }
        [DoNotSerialize] public ValueOutput h { get; private set; }

        protected override void Definition()
        {
            color = ValueInput(nameof(color), Color.white);
            l = ValueOutput(nameof(l), flow => Compute(flow).x).Predictable();
            c = ValueOutput(nameof(c), flow => Compute(flow).y).Predictable();
            h = ValueOutput(nameof(h), flow => Compute(flow).z).Predictable();
            Requirement(color, l);
            Requirement(color, c);
            Requirement(color, h);
        }

        private Vector3 Compute(Flow flow)
        {
            var rgb = flow.GetValue<Color>(color);
            return ToOkLCh(rgb.r, rgb.g, rgb.b);
        }

        public static Vector3 ToOkLCh(float r, float g, float b)
        {
            var lms_l = Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
            var lms_m = Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
            var lms_s = Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);

            var labL = 0.2104542553f * lms_l + 0.7936177850f * lms_m - 0.0040720468f * lms_s;
            var labA = 1.9779984951f * lms_l - 2.4285922050f * lms_m + 0.4505937099f * lms_s;
            var labB = 0.0259040371f * lms_l + 0.7827717662f * lms_m - 0.8086757660f * lms_s;

            return new Vector3(labL, Mathf.Sqrt(labA * labA + labB * labB), Mathf.Atan2(labB, labA));
        }

        private static float Cbrt(float x)
        {
            return x < 0f ? -Mathf.Pow(-x, 1f / 3f) : Mathf.Pow(x, 1f / 3f);
        }
    }

    /// <summary>
    /// KHR_interactivity math/rgbFromOkLCh: converts OkLCh to a linear RGB color (BT.709), without gamut clamping.
    /// Hue is in radians. Use Color.gamma afterwards for a color in gamma (sRGB) space.
    /// </summary>
    [UnitCategory("Math/Color")]
    [UnitTitle("RGB From OkLCh (glTF)")]
    public class GltfRgbFromOkLCh : Unit
    {
        [DoNotSerialize] public ValueInput l { get; private set; }
        [DoNotSerialize] public ValueInput c { get; private set; }
        [DoNotSerialize] public ValueInput h { get; private set; }
        [DoNotSerialize] public ValueInput alpha { get; private set; }
        [DoNotSerialize] public ValueOutput color { get; private set; }

        protected override void Definition()
        {
            l = ValueInput(nameof(l), 1f);
            c = ValueInput(nameof(c), 0f);
            h = ValueInput(nameof(h), 0f);
            alpha = ValueInput(nameof(alpha), 1f);
            color = ValueOutput(nameof(color), Compute).Predictable();
            Requirement(l, color);
            Requirement(c, color);
            Requirement(h, color);
            Requirement(alpha, color);
        }

        private Color Compute(Flow flow)
        {
            var rgb = FromOkLCh(flow.GetValue<float>(l), flow.GetValue<float>(c), flow.GetValue<float>(h));
            return new Color(rgb.x, rgb.y, rgb.z, flow.GetValue<float>(alpha));
        }

        public static Vector3 FromOkLCh(float l, float c, float h)
        {
            var a = c * Mathf.Cos(h);
            var b = c * Mathf.Sin(h);

            var lms_l = l + 0.3963377774f * a + 0.2158037573f * b;
            var lms_m = l - 0.1055613458f * a - 0.0638541728f * b;
            var lms_s = l - 0.0894841775f * a - 1.2914855480f * b;

            lms_l = lms_l * lms_l * lms_l;
            lms_m = lms_m * lms_m * lms_m;
            lms_s = lms_s * lms_s * lms_s;

            return new Vector3(
                +4.0767416621f * lms_l - 3.3077115913f * lms_m + 0.2309699292f * lms_s,
                -1.2684380046f * lms_l + 2.6097574011f * lms_m - 0.3413193965f * lms_s,
                -0.0041960863f * lms_l - 0.7034186147f * lms_m + 1.7076147010f * lms_s);
        }
    }
}
