using UnityEngine;

namespace Unity.VisualScripting
{
    public readonly struct BezierEasingPreset
    {
        public readonly string name;
        public readonly Vector2 point1;
        public readonly Vector2 point2;

        public BezierEasingPreset(string name, float x1, float y1, float x2, float y2)
        {
            this.name = name;
            this.point1 = new Vector2(x1, y1);
            this.point2 = new Vector2(x2, y2);
        }
    }

    /// <summary>
    /// Cubic Bezier easing presets for the KHR_interactivity interpolate nodes.
    ///
    /// The control points are the same ones CSS uses, because KHR_interactivity defines its easing
    /// as a CSS cubic-bezier(): p1/p2 are the two inner control points of a curve whose end points
    /// are implicitly (0,0) and (1,1). Only the x components are constrained to [0, 1] by the spec
    /// (that keeps the curve invertible), so the overshooting "Back" presets are valid.
    /// </summary>
    public static class BezierEasingPresets
    {
        public static readonly BezierEasingPreset[] All =
        {
            // CSS keywords
            new BezierEasingPreset("Linear",       0f,    0f,    1f,    1f),
            new BezierEasingPreset("Ease",         0.25f, 0.1f,  0.25f, 1f),
            new BezierEasingPreset("Ease In",      0.42f, 0f,    1f,    1f),
            new BezierEasingPreset("Ease Out",     0f,    0f,    0.58f, 1f),
            new BezierEasingPreset("Ease In Out",  0.42f, 0f,    0.58f, 1f),

            new BezierEasingPreset("In Sine",      0.12f, 0f,    0.39f, 0f),
            new BezierEasingPreset("Out Sine",     0.61f, 1f,    0.88f, 1f),
            new BezierEasingPreset("In Out Sine",  0.37f, 0f,    0.63f, 1f),

            new BezierEasingPreset("In Quad",      0.11f, 0f,    0.5f,  0f),
            new BezierEasingPreset("Out Quad",     0.5f,  1f,    0.89f, 1f),
            new BezierEasingPreset("In Out Quad",  0.45f, 0f,    0.55f, 1f),

            new BezierEasingPreset("In Cubic",     0.32f, 0f,    0.67f, 0f),
            new BezierEasingPreset("Out Cubic",    0.33f, 1f,    0.68f, 1f),
            new BezierEasingPreset("In Out Cubic", 0.65f, 0f,    0.35f, 1f),

            new BezierEasingPreset("In Quart",     0.5f,  0f,    0.75f, 0f),
            new BezierEasingPreset("Out Quart",    0.25f, 1f,    0.5f,  1f),
            new BezierEasingPreset("In Out Quart", 0.76f, 0f,    0.24f, 1f),

            new BezierEasingPreset("In Expo",      0.7f,  0f,    0.84f, 0f),
            new BezierEasingPreset("Out Expo",     0.16f, 1f,    0.3f,  1f),
            new BezierEasingPreset("In Out Expo",  0.87f, 0f,    0.13f, 1f),

            new BezierEasingPreset("In Circ",      0.55f, 0f,    1f,    0.45f),
            new BezierEasingPreset("Out Circ",     0f,    0.55f, 0.45f, 1f),
            new BezierEasingPreset("In Out Circ",  0.85f, 0f,    0.15f, 1f),

            new BezierEasingPreset("In Back",      0.36f, 0f,    0.66f, -0.56f),
            new BezierEasingPreset("Out Back",     0.34f, 1.56f, 0.64f, 1f),
            new BezierEasingPreset("In Out Back",  0.68f, -0.6f, 0.32f, 1.6f),
        };

        /// <summary>Returns the index into <see cref="All"/> matching the control points, or -1 for a custom curve.</summary>
        public static int IndexOf(Vector2 point1, Vector2 point2)
        {
            // The interpolate units default to (1,1),(1,1), which evaluates to exactly the same
            // identity easing as Linear. Report it as Linear rather than as an unnamed custom curve.
            if (point1 == Vector2.one && point2 == Vector2.one)
            {
                return 0;
            }

            for (int i = 0; i < All.Length; i++)
            {
                if ((All[i].point1 - point1).sqrMagnitude < 1e-8f &&
                    (All[i].point2 - point2).sqrMagnitude < 1e-8f)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
