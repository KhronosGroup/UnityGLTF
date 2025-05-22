namespace UnityGLTF.Interactivity.Playback.Tests
{
    public enum ComparisonType
    {
        Approximately = 0,
        Equals = 1,
        IsNaN = 2,
        IsInfinity = 3
    }
    public interface ISubGraph
    {
        bool hasBValue { get; }

        public (Node inputNode, Node outputNode) CreateSubGraph(Graph g);
    }

    public class ApproximatelySubGraph : ISubGraph
    {
        private readonly float _tolerance;
        public bool hasBValue => true;

        public ApproximatelySubGraph(float tolerance)
        {
            _tolerance = tolerance;
        }

        public (Node inputNode, Node outputNode) CreateSubGraph(Graph g)
        {
            var sub = g.CreateNode("math/sub");
            var abs = g.CreateNode("math/abs");
            var ge = g.CreateNode("math/ge");
            var branch = g.CreateNode("flow/branch");

            var branchConditionValue = branch.AddValue(ConstStrings.CONDITION, true);
            branchConditionValue.TryConnectToSocket(ge, ConstStrings.VALUE);

            ge.AddValue(ConstStrings.A, _tolerance);
            var geBValue = ge.AddValue(ConstStrings.B, 0);
            geBValue.TryConnectToSocket(abs, ConstStrings.VALUE);

            var absAValue = abs.AddValue(ConstStrings.A, 0);
            absAValue.TryConnectToSocket(sub, ConstStrings.VALUE);

            return (sub, branch);
        }
    }

    public class EqualSubGraph : ISubGraph
    {
        public bool hasBValue => true;

        public (Node inputNode, Node outputNode) CreateSubGraph(Graph g)
        {
            var eq = g.CreateNode("math/eq");
            var branch = g.CreateNode("flow/branch");

            var branchConditionValue = branch.AddValue(ConstStrings.CONDITION, true);
            branchConditionValue.TryConnectToSocket(eq, ConstStrings.VALUE);

            return (eq, branch);
        }
    }

    public class IsNaNSubGraph : ISubGraph
    {
        public bool hasBValue => false;

        public (Node inputNode, Node outputNode) CreateSubGraph(Graph g)
        {
            var eq = g.CreateNode("math/isnan");
            var branch = g.CreateNode("flow/branch");

            var branchConditionValue = branch.AddValue(ConstStrings.CONDITION, true);
            branchConditionValue.TryConnectToSocket(eq, ConstStrings.VALUE);

            return (eq, branch);
        }
    }

    public class IsInfSubGraph : ISubGraph
    {
        public bool hasBValue => false;


        public (Node inputNode, Node outputNode) CreateSubGraph(Graph g)
        {
            var eq = g.CreateNode("math/isinf");
            var branch = g.CreateNode("flow/branch");

            var branchConditionValue = branch.AddValue(ConstStrings.CONDITION, true);
            branchConditionValue.TryConnectToSocket(eq, ConstStrings.VALUE);

            return (eq, branch);
        }
    }
}