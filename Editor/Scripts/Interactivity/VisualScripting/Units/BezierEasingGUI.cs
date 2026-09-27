using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Draws the easing curve of the interpolate units, plus a preset picker.
    ///
    /// The curve is rasterized into a cached texture rather than drawn with Handles, because the
    /// graph canvas applies a zoom matrix to GUI space that Handles does not follow.
    /// </summary>
    public static class BezierEasingGUI
    {
        public const float PopupHeight = 18f;
        public const float SpaceBetween = 2f;
        public const float CurveHeight = 60f;
        public const float PreferredWidth = 150f;

        public static float GetHeight()
        {
            return PopupHeight + SpaceBetween + CurveHeight;
        }

        #region Preset popup

        private static GUIContent[] _popupContents;

        private static GUIContent[] popupContents
        {
            get
            {
                if (_popupContents == null)
                {
                    var presets = BezierEasingPresets.All;
                    _popupContents = new GUIContent[presets.Length + 1];
                    _popupContents[0] = new GUIContent("Custom");

                    for (int i = 0; i < presets.Length; i++)
                    {
                        _popupContents[i + 1] = new GUIContent(presets[i].name);
                    }
                }

                return _popupContents;
            }
        }

        /// <summary>
        /// Draws the preset popup. Returns true and writes the chosen control points to
        /// <paramref name="point1"/>/<paramref name="point2"/> when the user picks a preset.
        /// </summary>
        public static bool PresetPopup(Rect position, ref Vector2 point1, ref Vector2 point2)
        {
            var presetIndex = BezierEasingPresets.IndexOf(point1, point2);

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUI.Popup(position, presetIndex + 1, popupContents);

            if (!EditorGUI.EndChangeCheck() || newIndex == 0 || newIndex == presetIndex + 1)
            {
                return false;
            }

            var preset = BezierEasingPresets.All[newIndex - 1];
            point1 = preset.point1;
            point2 = preset.point2;
            return true;
        }

        #endregion

        #region Curve preview

        public static void CurveField(Rect position, Vector2 point1, Vector2 point2, bool dimmed)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var width = Mathf.Clamp(Mathf.RoundToInt(position.width), 8, 512);
            var height = Mathf.Clamp(Mathf.RoundToInt(position.height), 8, 512);

            var texture = GetCurveTexture(point1, point2, width, height);

            var tint = dimmed ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            GUI.DrawTexture(position, texture, ScaleMode.StretchToFill, true, 0f, tint, 0f, 0f);
        }

        /// <summary>
        /// KHR_interactivity requires the x component of both control points to be within [0, 1];
        /// outside that range the curve is not a function of the input progress and the node errors.
        /// </summary>
        public static bool IsValid(Vector2 point1, Vector2 point2)
        {
            return IsValidX(point1.x) && IsValidX(point2.x);
        }

        private static bool IsValidX(float x)
        {
            return !float.IsNaN(x) && !float.IsInfinity(x) && x >= 0f && x <= 1f;
        }

        #endregion

        #region Rasterization

        private readonly struct CurveKey
        {
            private readonly Vector2 point1;
            private readonly Vector2 point2;
            private readonly int width;
            private readonly int height;
            private readonly bool proSkin;

            public CurveKey(Vector2 point1, Vector2 point2, int width, int height, bool proSkin)
            {
                this.point1 = point1;
                this.point2 = point2;
                this.width = width;
                this.height = height;
                this.proSkin = proSkin;
            }

            public override int GetHashCode()
            {
                return point1.GetHashCode() ^ (point2.GetHashCode() << 2) ^ (width << 8) ^ (height << 16) ^ (proSkin ? 1 : 0);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CurveKey other)) return false;
                return point1 == other.point1 && point2 == other.point2 &&
                       width == other.width && height == other.height && proSkin == other.proSkin;
            }
        }

        private static readonly Dictionary<CurveKey, Texture2D> curveCache = new Dictionary<CurveKey, Texture2D>();
        private const int MaxCachedCurves = 64;

        private static Texture2D GetCurveTexture(Vector2 point1, Vector2 point2, int width, int height)
        {
            var key = new CurveKey(point1, point2, width, height, EditorGUIUtility.isProSkin);

            if (curveCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            if (curveCache.Count >= MaxCachedCurves)
            {
                foreach (var entry in curveCache.Values)
                {
                    if (entry != null) Object.DestroyImmediate(entry);
                }

                curveCache.Clear();
            }

            var texture = RenderCurve(point1, point2, width, height);
            curveCache[key] = texture;
            return texture;
        }

        private static Texture2D RenderCurve(Vector2 point1, Vector2 point2, int width, int height)
        {
            var pro = EditorGUIUtility.isProSkin;

            var background = pro ? new Color(0.16f, 0.16f, 0.16f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
            var grid = pro ? new Color(1f, 1f, 1f, 0.07f) : new Color(0f, 0f, 0f, 0.07f);
            var bounds = pro ? new Color(1f, 1f, 1f, 0.18f) : new Color(0f, 0f, 0f, 0.18f);
            var reference = pro ? new Color(1f, 1f, 1f, 0.14f) : new Color(0f, 0f, 0f, 0.14f);
            var curve = IsValid(point1, point2)
                ? new Color(0.36f, 0.79f, 0.98f, 1f)
                : new Color(0.95f, 0.42f, 0.36f, 1f);

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            // The y axis is padded so overshooting curves (the "Back" presets) stay inside the box.
            FindOutputRange(point1, point2, out var minValue, out var maxValue);
            var padding = Mathf.Max(0.04f, (maxValue - minValue) * 0.06f);
            minValue -= padding;
            maxValue += padding;

            float ToPixelX(float t) => t * (width - 1);
            float ToPixelY(float q) => (1f - Mathf.InverseLerp(minValue, maxValue, q)) * (height - 1);

            // Grid at quarters of the input range.
            for (int i = 1; i < 4; i++)
            {
                VerticalLine(pixels, width, height, ToPixelX(i / 4f), grid);
            }

            // The q = 0 and q = 1 bounds: the curve starts and ends on these.
            HorizontalLine(pixels, width, height, ToPixelY(0f), bounds);
            HorizontalLine(pixels, width, height, ToPixelY(1f), bounds);

            // Faint linear reference so the easing's shape reads at a glance.
            PlotLine(pixels, width, height, ToPixelX(0f), ToPixelY(0f), ToPixelX(1f), ToPixelY(1f), reference, 0.8f);

            var steps = Mathf.Max(512, width * 8);
            var previousX = ToPixelX(0f);
            var previousY = ToPixelY(InterpolateHelper.EvaluateEasing(point1, point2, 0f));

            for (int i = 1; i <= steps; i++)
            {
                var t = (float)i / steps;
                var x = ToPixelX(t);
                var y = ToPixelY(InterpolateHelper.EvaluateEasing(point1, point2, t));

                PlotLine(pixels, width, height, previousX, previousY, x, y, curve, 1.1f);

                previousX = x;
                previousY = y;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void FindOutputRange(Vector2 point1, Vector2 point2, out float min, out float max)
        {
            min = 0f;
            max = 1f;

            for (int i = 0; i <= 64; i++)
            {
                var q = InterpolateHelper.EvaluateEasing(point1, point2, i / 64f);
                if (float.IsNaN(q)) continue;
                if (q < min) min = q;
                if (q > max) max = q;
            }
        }

        private static void Blend(Color[] pixels, int width, int height, int x, int y, Color color, float coverage)
        {
            if (coverage <= 0f || x < 0 || y < 0 || x >= width || y >= height) return;

            var alpha = Mathf.Clamp01(coverage) * color.a;
            var index = y * width + x;
            pixels[index] = Color.Lerp(pixels[index], color, alpha);
        }

        private static void VerticalLine(Color[] pixels, int width, int height, float x, Color color)
        {
            var xi = Mathf.RoundToInt(x);
            for (int y = 0; y < height; y++)
            {
                Blend(pixels, width, height, xi, y, color, 1f);
            }
        }

        private static void HorizontalLine(Color[] pixels, int width, int height, float y, Color color)
        {
            var yi = Mathf.RoundToInt(y);
            for (int x = 0; x < width; x++)
            {
                Blend(pixels, width, height, x, yi, color, 1f);
            }
        }

        /// <summary>
        /// Splats a line segment by walking it in sub-pixel steps and stamping a soft round brush.
        /// Segments here are short (the curve is sampled densely), so this stays cheap.
        /// </summary>
        private static void PlotLine(Color[] pixels, int width, int height, float x0, float y0, float x1, float y1, Color color, float radius)
        {
            var distance = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.35f));

            for (int i = 0; i <= steps; i++)
            {
                var s = (float)i / steps;
                Splat(pixels, width, height, Mathf.Lerp(x0, x1, s), Mathf.Lerp(y0, y1, s), color, radius);
            }
        }

        private static void Splat(Color[] pixels, int width, int height, float x, float y, Color color, float radius)
        {
            var minX = Mathf.FloorToInt(x - radius);
            var maxX = Mathf.CeilToInt(x + radius);
            var minY = Mathf.FloorToInt(y - radius);
            var maxY = Mathf.CeilToInt(y + radius);

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    var dx = px - x;
                    var dy = py - y;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    Blend(pixels, width, height, px, py, color, radius - distance + 0.5f);
                }
            }
        }

        #endregion
    }
}
