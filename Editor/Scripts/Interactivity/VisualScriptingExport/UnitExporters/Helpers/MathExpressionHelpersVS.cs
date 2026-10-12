using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Input for a node created by <see cref="MathExpression"/>: a node output, a Visual Scripting value input or a constant.
    /// </summary>
    public readonly struct MathOperand
    {
        public readonly ValueOutRef source;
        public readonly IUnitInputPort port;
        public readonly object constant;

        private MathOperand(ValueOutRef source, IUnitInputPort port, object constant)
        {
            this.source = source;
            this.port = port;
            this.constant = constant;
        }

        public static implicit operator MathOperand(ValueOutRef source) => new MathOperand(source, null, null);
        public static implicit operator MathOperand(ValueInput port) => new MathOperand(null, port, null);
        public static implicit operator MathOperand(float value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(int value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(bool value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(Vector2 value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(Vector3 value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(Vector4 value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(Quaternion value) => new MathOperand(null, null, value);
        public static implicit operator MathOperand(Color value) => new MathOperand(null, null, value);
        public static MathOperand Constant(object value) => new MathOperand(null, null, value);

        public ValueInRef ConnectTo(ValueInRef socket)
        {
            if (source != null)
                socket.ConnectToSource(source);
            else if (port != null)
                socket.MapToInputPort(port);
            else
                socket.SetValue(constant);
            return socket;
        }
    }

    /// <summary>
    /// Builds small math expressions out of glTF math nodes. Inputs are connected to the sockets "a", "b", "c", "d" in order.
    /// </summary>
    public class MathExpression
    {
        private static readonly string[] SocketNames = { "a", "b", "c", "d" };

        public readonly INodeExporter exporter;
        private readonly TypeRestriction defaultRestriction;
        private readonly ExpectedType defaultExpectedType;

        /// <param name="restriction">Applied to every input socket, e.g. LimitToFloat to get int to float conversions.</param>
        /// <param name="expectedType">Applied to every output socket.</param>
        public MathExpression(INodeExporter exporter, TypeRestriction restriction = null, ExpectedType expectedType = null)
        {
            this.exporter = exporter;
            defaultRestriction = restriction;
            defaultExpectedType = expectedType;
        }

        public ValueOutRef Op<TSchema>(params MathOperand[] inputs) where TSchema : GltfInteractivityNodeSchema, new()
        {
            return Op<TSchema>(out _, inputs);
        }

        public ValueOutRef Op<TSchema>(out GltfInteractivityExportNode node, params MathOperand[] inputs) where TSchema : GltfInteractivityNodeSchema, new()
        {
            node = exporter.CreateNode<TSchema>();
            for (int i = 0; i < inputs.Length; i++)
            {
                var socket = inputs[i].ConnectTo(node.ValueIn(SocketNames[i]));
                if (defaultRestriction != null)
                    socket.SetType(defaultRestriction);
            }

            var output = node.FirstValueOut();
            if (defaultExpectedType != null)
                output.ExpectedType(defaultExpectedType);
            return output;
        }

        public ValueOutRef Add(MathOperand a, MathOperand b) => Op<Math_AddNode>(a, b);
        public ValueOutRef Sub(MathOperand a, MathOperand b) => Op<Math_SubNode>(a, b);
        public ValueOutRef Mul(MathOperand a, MathOperand b) => Op<Math_MulNode>(a, b);
        public ValueOutRef Div(MathOperand a, MathOperand b) => Op<Math_DivNode>(a, b);
        public ValueOutRef Min(MathOperand a, MathOperand b) => Op<Math_MinNode>(a, b);
        public ValueOutRef Max(MathOperand a, MathOperand b) => Op<Math_MaxNode>(a, b);
        public ValueOutRef Abs(MathOperand a) => Op<Math_AbsNode>(a);
        public ValueOutRef Clamp(MathOperand a, MathOperand min, MathOperand max) => Op<Math_ClampNode>(a, min, max);
        public ValueOutRef Mix(MathOperand a, MathOperand b, MathOperand t) => Op<Math_MixNode>(a, b, t);
        public ValueOutRef Sqrt(MathOperand a) => Op<Math_SqrtNode>(a);

        /// <summary> Boolean operations always use bool restrictions and outputs. </summary>
        public ValueOutRef Compare<TSchema>(MathOperand a, MathOperand b) where TSchema : GltfInteractivityNodeSchema, new()
        {
            var node = exporter.CreateNode<TSchema>();
            var socketA = a.ConnectTo(node.ValueIn("a"));
            var socketB = b.ConnectTo(node.ValueIn("b"));
            if (defaultRestriction != null)
            {
                socketA.SetType(defaultRestriction);
                socketB.SetType(defaultRestriction);
            }
            return node.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }

        public ValueOutRef Not(MathOperand a)
        {
            var node = exporter.CreateNode<Math_NotNode>();
            a.ConnectTo(node.ValueIn(Math_NotNode.IdValueA)).SetType(TypeRestriction.LimitToBool);
            return node.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }

        public ValueOutRef And(MathOperand a, MathOperand b)
        {
            var node = exporter.CreateNode<Math_AndNode>();
            a.ConnectTo(node.ValueIn(Math_AndNode.IdValueA)).SetType(TypeRestriction.LimitToBool);
            b.ConnectTo(node.ValueIn(Math_AndNode.IdValueB)).SetType(TypeRestriction.LimitToBool);
            return node.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }

        public ValueOutRef Or(MathOperand a, MathOperand b)
        {
            var node = exporter.CreateNode<Math_OrNode>();
            a.ConnectTo(node.ValueIn(Math_OrNode.IdValueA)).SetType(TypeRestriction.LimitToBool);
            b.ConnectTo(node.ValueIn(Math_OrNode.IdValueB)).SetType(TypeRestriction.LimitToBool);
            return node.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }

        /// <summary> condition ? a : b </summary>
        public ValueOutRef Select(MathOperand condition, MathOperand a, MathOperand b)
        {
            var node = exporter.CreateNode<Math_SelectNode>();
            condition.ConnectTo(node.ValueIn(Math_SelectNode.IdCondition)).SetType(TypeRestriction.LimitToBool);
            var socketA = a.ConnectTo(node.ValueIn(Math_SelectNode.IdValueA));
            var socketB = b.ConnectTo(node.ValueIn(Math_SelectNode.IdValueB));
            if (defaultRestriction != null)
            {
                socketA.SetType(defaultRestriction);
                socketB.SetType(defaultRestriction);
            }
            var output = node.FirstValueOut();
            if (defaultExpectedType != null)
                output.ExpectedType(defaultExpectedType);
            return output;
        }

        /// <summary> Dot product, always a float output. </summary>
        public ValueOutRef Dot(MathOperand a, MathOperand b)
        {
            var node = exporter.CreateNode<Math_DotNode>();
            var socketA = a.ConnectTo(node.ValueIn(Math_DotNode.IdValueA));
            var socketB = b.ConnectTo(node.ValueIn(Math_DotNode.IdValueB));
            if (defaultRestriction != null)
            {
                socketA.SetType(defaultRestriction);
                socketB.SetType(defaultRestriction);
            }
            return node.FirstValueOut().ExpectedType(ExpectedType.Float);
        }

        /// <summary> Length of a vector, always a float output. </summary>
        public ValueOutRef Length(MathOperand a)
        {
            var node = exporter.CreateNode<Math_LengthNode>();
            var socketA = a.ConnectTo(node.ValueIn(Math_LengthNode.IdValueA));
            if (defaultRestriction != null)
                socketA.SetType(defaultRestriction);
            return node.FirstValueOut().ExpectedType(ExpectedType.Float);
        }

        /// <summary> Creates an expression for scalar float math. </summary>
        public static MathExpression Float(INodeExporter exporter)
        {
            return new MathExpression(exporter, TypeRestriction.LimitToFloat, ExpectedType.Float);
        }

        /// <summary> Creates an expression for the given glTF type (e.g. float3 for Vector3 math). </summary>
        public static MathExpression OfType(INodeExporter exporter, int gltfTypeIndex)
        {
            return new MathExpression(exporter, TypeRestriction.LimitToType(gltfTypeIndex), ExpectedType.GtlfType(gltfTypeIndex));
        }

        public static MathExpression OfType(INodeExporter exporter, System.Type type)
        {
            return OfType(exporter, GltfTypes.TypeIndex(type));
        }

        /// <summary>
        /// Returns a constant of the given vector type with all components set to value (float for scalars).
        /// </summary>
        public static object Splat(System.Type type, float value)
        {
            if (type == typeof(Vector2))
                return new Vector2(value, value);
            if (type == typeof(Vector3))
                return new Vector3(value, value, value);
            if (type == typeof(Vector4))
                return new Vector4(value, value, value, value);
            return value;
        }
    }
}
