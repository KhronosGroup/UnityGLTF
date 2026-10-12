using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerGet : BehaviourEngineNode
    {
        private readonly PointerAccess _access;

        public PointerGet(BehaviourEngine engine, Node node) : base(engine, node)
        {
            PointerAccess.TryCreate(this, out _access);
        }

        public override Variant GetOutputValue(string id)
        {
            IPointer pointer = null;
            var valid = _access != null && _access.TryResolve(this, out pointer, out _);

            switch (id)
            {
                case ConstStrings.VALUE:
                    if (!valid)
                        return _access == null ? Variant.FromFloat(float.NaN) : Variant.Default(_access.type);
                    return PointerHelpers.Read(pointer);

                case ConstStrings.IS_VALID:
                    return Variant.FromBool(valid);
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }
    }
}
