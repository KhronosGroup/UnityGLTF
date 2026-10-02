namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>ref/eq: true when both references are null or refer to the same object, existing or not.</summary>
    public class RefEq : BehaviourEngineNode
    {
        public RefEq(BehaviourEngine engine, Node node) : base(engine, node) { }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Ref a);
            TryEvaluateValue(ConstStrings.B, out Ref b);

            return new Property<bool>(a == b);
        }
    }
}
