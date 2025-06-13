namespace UnityGLTF.Interactivity.Playback
{
    public class MathRandom : BehaviourEngineNode
    {
        private bool _flowTriggered;
        private float _randomValue;

        public MathRandom(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Only generate a new value if a flow has been triggered as that's the spec.
            engine.onFlowTriggered += OnFlowTriggered;
        }

        private void OnFlowTriggered(Flow flow)
        {
            _flowTriggered = true;
        }

        public override IProperty GetOutputValue(string id)
        {
            if (_flowTriggered)
            {
                _randomValue = UnityEngine.Random.value;
                _flowTriggered = false;
            }

            return new Property<float>(_randomValue);
        }
    }
}