using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerSet : BehaviourEngineNode
    {
        private readonly PointerAccess _access;

        public PointerSet(BehaviourEngine engine, Node node) : base(engine, node)
        {
            PointerAccess.TryCreate(this, out _access);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // Evaluate all input values, then resolve. Any failure activates "err".
            if (_access == null || !TryEvaluateValue(ConstStrings.VALUE, out IProperty value) ||
                !_access.TryResolve(this, out var pointer, out var effectivePointer) || PointerHelpers.IsReadOnly(pointer))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            engine.pointerInterpolationManager.StopInterpolation(effectivePointer);

            if (!PointerHelpers.TryWrite(pointer, value))
            {
                Debug.LogWarning($"pointer/set: value type {value.GetTypeSignature()} does not match pointer type {pointer.GetSystemType()}.");
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
