using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSequence : BehaviourEngineNode
    {
        /// <summary>Indices of the output flows in socket order.</summary>
        private readonly int[] _orderedFlows;

        public FlowSequence(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _orderedFlows = new int[node.flows.Count];

            for (int i = 0; i < _orderedFlows.Length; i++)
            {
                _orderedFlows[i] = i;
                Util.Log($"{node.flows[i].fromSocket}");
            }

            Array.Sort(_orderedFlows, (a, b) => node.flows[a].CompareTo(node.flows[b]));
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            for (int i = 0; i < _orderedFlows.Length; i++)
            {
                TryExecuteFlow(_orderedFlows[i]);
            }
        }
    }
}
