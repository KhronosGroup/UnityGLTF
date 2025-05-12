using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class NoOp : BehaviourEngineNode
    {
        private Declaration _declaration;

        public NoOp(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _declaration = FindDeclaration(node.type, engine.graph);
        }

        public override IProperty GetOutputValue(string id)
        {
            Util.Log($"Checking NoOP node for value {id}");

            var value = FindValueSocket(id);

            return engine.graph.GetDefaultPropertyForType(value.type);
        }

        private ValueSocket FindValueSocket(string id)
        {
            if (_declaration.outputValueSockets == null || _declaration.outputValueSockets.Count <= 0)
                throw new InvalidOperationException($"Attempting to find value socket {id} on this NoOp node when it has no output value sockets!");

            for (int i = 0; i < _declaration.outputValueSockets.Count; i++)
            {
                if (_declaration.outputValueSockets[i].name.Equals(id))
                    return _declaration.outputValueSockets[i];
            }

            throw new InvalidOperationException($"No value socket found for {id}!");
        }

        private static Declaration FindDeclaration(string op, Graph graph)
        {
            for (int i = 0; i < graph.declarations.Count; i++)
            {
                if (graph.declarations[i].op.Equals(op))
                    return graph.declarations[i];
            }

            throw new InvalidOperationException($"No declaration found for operation {op}!");
        }
    }
}