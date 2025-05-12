using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerInterpolate : BehaviourEngineNode
    {
        private IPointer _pointer;
        private IProperty _interpGoal;
        private float _duration;
        private float2 _p1, _p2;

        public PointerInterpolate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if(validationResult != ValidationResult.Valid)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            var data = new PointerInterpolateData()
            {
                pointer = _pointer,
                startTime = Time.time,
                duration = _duration,
                endValue = _interpGoal,
                cp1 = _p1,
                cp2 = _p2,
                done = () => TryExecuteFlow(ConstStrings.DONE)
            };

            try
            {
                engine.pointerInterpolationManager.StartInterpolation(ref data);

                TryExecuteFlow(ConstStrings.OUT);
            }
            catch(InterpolatorException ex)
            {
                Debug.LogWarning(ex);
                TryExecuteFlow(ConstStrings.ERR);
            }
        }

        public override bool ValidateConfiguration(string socket)
        {
            return TryGetPointerFromConfiguration(out _pointer) && 
                _pointer is not IReadOnlyPointer;
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.VALUE, out _interpGoal) && 
                TryEvaluateValue(ConstStrings.DURATION, out _duration) &&
                DurationIsValid(_duration) &&
                TryEvaluateValue(ConstStrings.P1, out _p1) &&
                ControlPointIsValid(_p1) &&
                TryEvaluateValue(ConstStrings.P2, out _p2) &&
                ControlPointIsValid(_p2);
        }

        private static bool DurationIsValid(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0)
                return false;

            return true;
        }

        private static bool ControlPointIsValid(float2 cp)
        {
            if (IsInvalid(cp.x))
                return false;

            if (IsInvalid(cp.y))
                return false;

            return true;

            bool IsInvalid(float v)
            {
                return float.IsNaN(v) || float.IsInfinity(v) || v < 0 || v > 1;
            }
        }
    }
}