using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNaN : BehaviourEngineNode
    {
        public MathNaN(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            return Variant.FromFloat(math.NAN);
        }
    }
}