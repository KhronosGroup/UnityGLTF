using Unity.Mathematics;

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

    public static class QuaternionSubGraph
    {
        private const float EQUIVALENT_QUAT_DOT_PRODUCT = 1f;

        public static (Node inputNode, Node outputNode) CreateSubGraph(Graph g, float tolerance, float4 expected)
        {
            // In flow order
            var norm = g.CreateNode("math/normalize");
            var dot = g.CreateNode("math/dot");
            var abs = g.CreateNode("math/abs");
            var sub = g.CreateNode("math/sub");
            var le = g.CreateNode("math/le");
            var branch = g.CreateNode("flow/branch");

            dot.AddConnectedValue(ConstStrings.A, norm);
            dot.AddValue(ConstStrings.B, expected);

            abs.AddConnectedValue(ConstStrings.A, dot);

            sub.AddValue(ConstStrings.A, EQUIVALENT_QUAT_DOT_PRODUCT);
            sub.AddConnectedValue(ConstStrings.B, abs);

            le.AddConnectedValue(ConstStrings.A, sub);
            le.AddValue(ConstStrings.B, tolerance);

            branch.AddConnectedValue(ConstStrings.CONDITION, le);

            return (norm, branch);
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
            var eq = g.CreateNode("math/isNaN");
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
            var eq = g.CreateNode("math/isInf");
            var branch = g.CreateNode("flow/branch");

            var branchConditionValue = branch.AddValue(ConstStrings.CONDITION, true);
            branchConditionValue.TryConnectToSocket(eq, ConstStrings.VALUE);

            return (eq, branch);
        }
    }
}