
using UnityEngine;

namespace Unity.VisualScripting
{
    public static class InterpolateHelper
    {
        public static bool AreValuesEqual(object valueA, object valueB)
        {
            switch (valueA)
            {
                case float f:
                    if (Mathf.Abs((float)valueB - f) > 0.0001f)
                    {
                        return false;
                    }

                    break;
                case Vector2 v2:
                    var diff2 = v2 - (Vector2)valueB;
                    if (diff2.magnitude > 0.0001f)
                        return false;
                    break;
                case Vector3 v3:
                    var diff3 = v3 - (Vector3)valueB;
                    if (diff3.magnitude > 0.0001f)
                        return false;
                    break;
                case Vector4 v4:
                    var diff4 = v4 - (Vector4)valueB;
                    if (diff4.magnitude > 0.0001f)
                        return false;
                    break;
                case Quaternion q:
                    var diffQ = q.eulerAngles - ((Quaternion)valueB).eulerAngles;
                    if (diffQ.magnitude > 0.0001f)
                        return false;
                    break;
                case Color c:
                    var diff = c - (Color)valueB;
                    float d = Mathf.Abs(diff.r) + Mathf.Abs(diff.g) + Mathf.Abs(diff.b) + Mathf.Abs(diff.a);
                    if (d > 0.0001f)
                        return false;
                    break;
                case Matrix4x4 m:
                    for (int i = 0; i < 4; i++)
                    {
                        var diffM = m.GetColumn(0) - ((Matrix4x4)valueB).GetColumn(0);
                        if (diffM.magnitude > 0.0001f)
                            return false;
                    }
                    break;
            }

            return true;
        }
        
        // Evaluates one axis of a cubic Bezier with the implicit end points P0 = 0 and P3 = 1.
        private static float BezierAxis(float s, float c1, float c2)
        {
            float u = 1f - s;
            return 3f * u * u * s * c1 + 3f * u * s * s * c2 + s * s * s;
        }

        private static float BezierAxisDerivative(float s, float c1, float c2)
        {
            float u = 1f - s;
            return 3f * u * u * c1 + 6f * u * s * (c2 - c1) + 3f * s * s * (1f - c2);
        }

        // Maps an input progress x to the curve parameter s by solving X(s) == x, as CSS
        // cubic-bezier() easing requires. KHR_interactivity constrains p1.x and p2.x to [0, 1],
        // which keeps X monotonic on [0, 1] and therefore invertible.
        private static float SolveForCurveParameter(float x, float c1, float c2)
        {
            float s = x;
            for (int i = 0; i < 8; i++)
            {
                float error = BezierAxis(s, c1, c2) - x;
                if (Mathf.Abs(error) < 1e-6f)
                    return s;

                float derivative = BezierAxisDerivative(s, c1, c2);
                if (Mathf.Abs(derivative) < 1e-6f)
                    break;

                s -= error / derivative;
            }

            // Newton-Raphson did not converge (near-flat slope), fall back to bisection.
            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 32; i++)
            {
                s = (low + high) * 0.5f;
                if (BezierAxis(s, c1, c2) < x)
                    low = s;
                else
                    high = s;
            }

            return s;
        }

        /// <summary>
        /// Maps an input progress to the eased output progress, the same way CSS cubic-bezier() does.
        /// This is the mapping <see cref="BezierInterpolate"/> applies before lerping, exposed so that
        /// editor previews show exactly what the runtime evaluates.
        /// </summary>
        public static float EvaluateEasing(Vector2 pointAValue, Vector2 pointBValue, float f)
        {
            var s = SolveForCurveParameter(f, pointAValue.x, pointBValue.x);
            return BezierAxis(s, pointAValue.y, pointBValue.y);
        }

        public static object BezierInterpolate(Vector2 pointAValue, Vector2 pointBValue, object currentValue, object targetValue, float f)
        {
            f = EvaluateEasing(pointAValue, pointBValue, f);
            if (currentValue is Vector2 currentVector2)
            {
                return Vector2.Lerp(currentVector2, (Vector2)targetValue , f);
            }
            else if (currentValue is Vector3 currentVector3)
            {
                return Vector3.Lerp(currentVector3, (Vector3)targetValue , f);
            }
            else if (currentValue is Vector4 currentVector4)
            {
                return Vector4.Lerp(currentVector4, (Vector4)targetValue , f);
            }
            else if (currentValue is Quaternion currentQuaternion)
            {
                return Quaternion.Slerp(currentQuaternion, (Quaternion)targetValue , f);
            }
            else if (currentValue is Color currentColor)
            {
                return Color.Lerp(currentColor, (Color)targetValue , f);
            }
            else if (currentValue is int currentInt)
            {
                return Mathf.RoundToInt(Mathf.Lerp(currentInt, (int)targetValue , f));
            }
            else if (currentValue is float currentFloat)
            {
                return Mathf.Lerp(currentFloat, (float)targetValue , f);
            }
            return currentValue;
        }
        
    }
}