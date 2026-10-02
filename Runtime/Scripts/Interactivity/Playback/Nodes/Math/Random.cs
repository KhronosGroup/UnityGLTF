namespace UnityGLTF.Interactivity.Playback
{
    public class MathRandom : BehaviourEngineNode
    {
        // A dedicated generator so graphs don't consume or reseed UnityEngine.Random's global state.
        private static readonly System.Random _rng = new();

        public MathRandom(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        // The engine retains output values until a flow activation, so a new value is
        // generated only on the first access after each flow activation.
        public override IProperty GetOutputValue(string id)
        {
            // NextDouble is in [0, 1); the float cast can round up to 1, which the spec excludes.
            var v = (float)_rng.NextDouble();
            return new Property<float>(v >= 1f ? 0f : v);
        }
    }
}
