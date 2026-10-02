using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerInterpolate : BehaviourEngineNode
    {
        private readonly PointerAccess _access;

        public PointerInterpolate(BehaviourEngine engine, Node node) : base(engine, node)
        {
            PointerAccess.TryCreate(this, out _access);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // Step 1: evaluate all input values.
            if (_access == null ||
                !TryEvaluateValue(ConstStrings.VALUE, out IProperty target) ||
                !TryEvaluateValue(ConstStrings.DURATION, out float duration) ||
                !TryEvaluateValue(ConstStrings.P1, out float2 p1) ||
                !TryEvaluateValue(ConstStrings.P2, out float2 p2))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            // Steps 2-4: parameters, resolution, type and mutability.
            if (!_access.TryResolve(this, out var pointer, out var effectivePointer) || PointerHelpers.IsReadOnly(pointer))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            // Steps 5-6: duration and control points.
            if (!InterpolationRules.IsValidDuration(duration) || !InterpolationRules.IsValidControlPoint(p1) || !InterpolationRules.IsValidControlPoint(p2))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            var data = new PointerInterpolateData()
            {
                pointerKey = effectivePointer,
                pointer = pointer,
                startTime = engine.time,
                duration = duration,
                endValue = target,
                cp1 = p1,
                cp2 = p2,
                done = () => TryExecuteFlow(ConstStrings.DONE)
            };

            try
            {
                engine.pointerInterpolationManager.StartInterpolation(ref data);
            }
            catch (InterpolatorException ex)
            {
                Debug.LogWarning(ex.Message);
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }
    }

    /// <summary>Input checks shared by pointer/interpolate and variable/interpolate.</summary>
    internal static class InterpolationRules
    {
        public static bool IsValidDuration(float duration)
        {
            return !float.IsNaN(duration) && !float.IsInfinity(duration) && duration >= 0;
        }

        /// <summary>Both components must be finite; only the X component is restricted to [0, 1].</summary>
        public static bool IsValidControlPoint(float2 cp)
        {
            if (!math.all(math.isfinite(cp)))
                return false;

            return cp.x >= 0f && cp.x <= 1f;
        }
    }
}
