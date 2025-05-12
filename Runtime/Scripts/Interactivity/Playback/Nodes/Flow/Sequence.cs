using System;
using System.Threading;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSequence : BehaviourEngineNode
    {
        private readonly Flow[] _orderedFlows;

        public FlowSequence(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _orderedFlows = new Flow[node.flows.Count];

            for (int i = 0; i < _orderedFlows.Length; i++)
            {
                _orderedFlows[i] = node.flows[i];
                Util.Log($"{node.flows[i].fromSocket}");
            }

            Array.Sort(_orderedFlows, (a, b) => a.CompareTo(b));
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            for (int i = 0; i < node.flows.Count; i++)
            {
                engine.ExecuteFlow(_orderedFlows[i]);
            }
        }
    }
}