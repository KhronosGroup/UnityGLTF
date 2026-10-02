using System;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Stand-in for unsupported operations: input flows are ignored, output flows never activate,
    /// and output values are constant type-defaults.
    /// </summary>
    public class NoOp : BehaviourEngineNode
    {
        private readonly Declaration _declaration;

        public NoOp(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _declaration = node.declaration ?? FindDeclaration(node.type, engine.graph);
        }

        public override IProperty GetOutputValue(string id)
        {
            Util.Log($"Checking NoOP node for value {id}");

            if (TryGetDeclaredOutputType(id, out var typeIndex))
                return engine.graph.GetDefaultPropertyForType(typeIndex);

            // Core operations don't list their sockets in the declaration, so fall back to the operation's spec.
            if (NodeRegistry.nodeSpecs.TryGetValue(node.type, out var spec))
            {
                var outputs = spec.GetOutputs().values;

                for (int i = 0; outputs != null && i < outputs.Length; i++)
                {
                    if (outputs[i].id == id && outputs[i].types != null && outputs[i].types.Length > 0)
                        return Helpers.GetDefaultProperty(outputs[i].types[0]);
                }
            }

            throw new InvalidOperationException($"No value socket {id} is known for the unsupported operation {node.type}.");
        }

        private bool TryGetDeclaredOutputType(string id, out int typeIndex)
        {
            typeIndex = -1;

            if (_declaration?.outputValueSockets == null)
                return false;

            for (int i = 0; i < _declaration.outputValueSockets.Count; i++)
            {
                if (_declaration.outputValueSockets[i].name.Equals(id))
                {
                    typeIndex = _declaration.outputValueSockets[i].type;
                    return true;
                }
            }

            return false;
        }

        private static Declaration FindDeclaration(string op, Graph graph)
        {
            for (int i = 0; i < graph.declarations.Count; i++)
            {
                if (graph.declarations[i].op.Equals(op))
                    return graph.declarations[i];
            }

            return null;
        }
    }
}
