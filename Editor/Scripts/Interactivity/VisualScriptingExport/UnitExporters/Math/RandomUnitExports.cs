using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;
using Random = UnityEngine.Random;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Random.value, Random.rotation, Random.rotationUniform and Random.insideUnitCircle based on math/random.
    /// </summary>
    public static class RandomUnitExports
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberExpressionUnitExport.Register(typeof(Random), nameof(Random.value), (e, unit, target) =>
                RandomValue(e));
            GetMemberExpressionUnitExport.Register(typeof(Random), nameof(Random.rotation), (e, unit, target) =>
                RandomRotation(e));
            GetMemberExpressionUnitExport.Register(typeof(Random), nameof(Random.rotationUniform), (e, unit, target) =>
                RandomRotation(e));
            GetMemberExpressionUnitExport.Register(typeof(Random), nameof(Random.insideUnitCircle), (e, unit, target) =>
                RandomInsideUnitCircle(e));
        }

        private static ValueOutRef RandomValue(INodeExporter exporter)
        {
            return exporter.CreateNode<Math_RandomNode>().FirstValueOut().ExpectedType(ExpectedType.Float);
        }

        /// <summary> Uniformly distributed rotation (Shoemake). </summary>
        private static ValueOutRef RandomRotation(INodeExporter exporter)
        {
            /*
                q = (sqrt(1 - u1) * sin(2PI * u2), sqrt(1 - u1) * cos(2PI * u2),
                     sqrt(u1) * sin(2PI * u3),     sqrt(u1) * cos(2PI * u3))
             */
            var m = MathExpression.Float(exporter);
            var u1 = RandomValue(exporter);
            var angle2 = m.Mul(RandomValue(exporter), Mathf.PI * 2f);
            var angle3 = m.Mul(RandomValue(exporter), Mathf.PI * 2f);
            var r1 = m.Sqrt(m.Sub(1f, u1));
            var r2 = m.Sqrt(u1);

            var combine = exporter.CreateNode<Math_Combine4Node>();
            combine.ValueIn(Math_Combine4Node.IdValueA).ConnectToSource(m.Mul(r1, m.Op<Math_SinNode>(angle2)));
            combine.ValueIn(Math_Combine4Node.IdValueB).ConnectToSource(m.Mul(r1, m.Op<Math_CosNode>(angle2)));
            combine.ValueIn(Math_Combine4Node.IdValueC).ConnectToSource(m.Mul(r2, m.Op<Math_SinNode>(angle3)));
            combine.ValueIn(Math_Combine4Node.IdValueD).ConnectToSource(m.Mul(r2, m.Op<Math_CosNode>(angle3)));
            return combine.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }

        /// <summary> Uniformly distributed point inside the unit circle: sqrt(u1) * (cos, sin)(2PI * u2) </summary>
        private static ValueOutRef RandomInsideUnitCircle(INodeExporter exporter)
        {
            var m = MathExpression.Float(exporter);
            var radius = m.Sqrt(RandomValue(exporter));
            var angle = m.Mul(RandomValue(exporter), Mathf.PI * 2f);

            var combine = exporter.CreateNode<Math_Combine2Node>();
            combine.ValueIn(Math_Combine2Node.IdValueA).ConnectToSource(m.Mul(radius, m.Op<Math_CosNode>(angle)));
            combine.ValueIn(Math_Combine2Node.IdValueB).ConnectToSource(m.Mul(radius, m.Op<Math_SinNode>(angle)));
            return combine.FirstValueOut().ExpectedType(ExpectedType.Float2);
        }
    }
}
