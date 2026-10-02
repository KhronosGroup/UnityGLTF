using System;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Shared Object Model access for pointer/get, pointer/set and pointer/interpolate:
    /// the parsed template, the configured type, and runtime resolution of the effective pointer.
    /// </summary>
    internal sealed class PointerAccess
    {
        public readonly PointerTemplate template;
        /// <summary>The value type selected by the "type" configuration.</summary>
        public readonly Type type;
        public readonly int typeIndex;

        private readonly int[] _ints;
        private readonly Ref[] _refs;

        private PointerAccess(PointerTemplate template, Type type, int typeIndex)
        {
            this.template = template;
            this.type = type;
            this.typeIndex = typeIndex;
            _ints = new int[template.parameters.Count];
            _refs = new Ref[template.parameters.Count];
        }

        /// <summary>Reads and checks the node configuration. Graph validation guarantees success for loaded graphs.</summary>
        public static bool TryCreate(BehaviourEngineNode node, out PointerAccess access)
        {
            access = null;

            if (!node.TryGetConfig(ConstStrings.POINTER, out string pointer) || !PointerTemplate.TryParse(pointer, out var template, out _))
                return false;

            var types = node.engine.graph.types;

            if (!node.TryGetConfig(ConstStrings.TYPE, out int typeIndex) || typeIndex < 0 || typeIndex >= types.Count)
                return false;

            access = new PointerAccess(template, Helpers.GetSystemType(types[typeIndex]), typeIndex);
            return true;
        }

        /// <summary>
        /// Evaluates template parameters and resolves the effective pointer. Fails when an integer parameter is
        /// negative, a reference parameter is null, the pointer does not resolve, or its type differs from <see cref="type"/>.
        /// </summary>
        public bool TryResolve(BehaviourEngineNode node, out IPointer pointer, out string effectivePointer)
        {
            pointer = default;
            effectivePointer = null;

            var parameters = template.parameters;

            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].isReference)
                {
                    if (!node.TryEvaluateValue(parameters[i].id, out Ref r))
                        return false;
                    _refs[i] = r;
                }
                else
                {
                    if (!node.TryEvaluateValue(parameters[i].id, out int v))
                        return false;
                    _ints[i] = v;
                }
            }

            if (!template.TryGenerate(_ints, _refs, out effectivePointer))
                return false;

            if (!node.engine.TryGetPointer(effectivePointer, node, out pointer))
                return false;

            return PointerHelpers.GetSpecType(pointer) == type;
        }
    }
}
