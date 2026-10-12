using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Unity color functions built from glTF math nodes: gamma/linear conversion, grayscale, max component, HSV.
    /// The formulas follow Unity's implementations incl. values outside [0, 1] (verified against Unity 2022.3).
    /// </summary>
    public static class ColorFunctions
    {
        /// <summary> Mathf.GammaToLinearSpace </summary>
        public static ValueOutRef GammaToLinear(INodeExporter exporter, MathOperand v)
        {
            var m = MathExpression.Float(exporter);
            var curve = m.Op<Math_PowNode>(m.Div(m.Add(v, 0.055f), 1.055f), 2.4f);
            var above1 = m.Select(m.Compare<Math_EqNode>(v, 1f), 1f, m.Op<Math_PowNode>(v, 2.2f));
            return m.Select(m.Compare<Math_LeNode>(v, 0.04045f), m.Div(v, 12.92f),
                m.Select(m.Compare<Math_LtNode>(v, 1f), curve, above1));
        }

        /// <summary> Mathf.LinearToGammaSpace </summary>
        public static ValueOutRef LinearToGamma(INodeExporter exporter, MathOperand v)
        {
            var m = MathExpression.Float(exporter);
            var curve = m.Sub(m.Mul(1.055f, m.Op<Math_PowNode>(v, 0.4166667f)), 0.055f);
            var above1 = m.Select(m.Compare<Math_EqNode>(v, 1f), 1f, m.Op<Math_PowNode>(v, 0.45454545f));
            return m.Select(m.Compare<Math_LeNode>(v, 0f), 0f,
                m.Select(m.Compare<Math_LeNode>(v, 0.0031308f), m.Mul(12.92f, v),
                    m.Select(m.Compare<Math_LtNode>(v, 1f), curve, above1)));
        }

        /// <summary> Splits a color into its components. </summary>
        public static GltfInteractivityExportNode Extract(INodeExporter exporter, MathOperand color,
            out ValueOutRef r, out ValueOutRef g, out ValueOutRef b, out ValueOutRef a)
        {
            var extract = exporter.CreateNode<Math_Extract4Node>();
            color.ConnectTo(extract.ValueIn(Math_Extract4Node.IdValueIn)).SetType(TypeRestriction.LimitToFloat4);
            r = extract.ValueOut(Math_Extract4Node.IdValueOutX).ExpectedType(ExpectedType.Float);
            g = extract.ValueOut(Math_Extract4Node.IdValueOutY).ExpectedType(ExpectedType.Float);
            b = extract.ValueOut(Math_Extract4Node.IdValueOutZ).ExpectedType(ExpectedType.Float);
            a = extract.ValueOut(Math_Extract4Node.IdValueOutW).ExpectedType(ExpectedType.Float);
            return extract;
        }

        public static ValueOutRef Combine(INodeExporter exporter, MathOperand r, MathOperand g, MathOperand b, MathOperand a)
        {
            var combine = exporter.CreateNode<Math_Combine4Node>();
            r.ConnectTo(combine.ValueIn(Math_Combine4Node.IdValueA)).SetType(TypeRestriction.LimitToFloat);
            g.ConnectTo(combine.ValueIn(Math_Combine4Node.IdValueB)).SetType(TypeRestriction.LimitToFloat);
            b.ConnectTo(combine.ValueIn(Math_Combine4Node.IdValueC)).SetType(TypeRestriction.LimitToFloat);
            a.ConnectTo(combine.ValueIn(Math_Combine4Node.IdValueD)).SetType(TypeRestriction.LimitToFloat);
            return combine.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }

        /// <summary> Applies a function to r, g and b, alpha is kept. </summary>
        public static ValueOutRef PerChannel(INodeExporter exporter, MathOperand color, Func<ValueOutRef, ValueOutRef> function)
        {
            Extract(exporter, color, out var r, out var g, out var b, out var a);
            return Combine(exporter, function(r), function(g), function(b), a);
        }

        /// <summary> Color.grayscale: 0.299 * r + 0.587 * g + 0.114 * b </summary>
        public static ValueOutRef Grayscale(INodeExporter exporter, MathOperand color)
        {
            return MathExpression.OfType(exporter, UnityMathExpressions.Float4).Dot(color, new Vector4(0.299f, 0.587f, 0.114f, 0f));
        }

        /// <summary> Color.maxColorComponent: Max(Max(r, g), b) </summary>
        public static ValueOutRef MaxColorComponent(INodeExporter exporter, MathOperand color)
        {
            var m = MathExpression.Float(exporter);
            Extract(exporter, color, out var r, out var g, out var b, out _);
            return m.Max(m.Max(r, g), b);
        }

        /// <summary> Color.HSVToRGB(H, S, V, hdr), alpha is 1. Without hdr the result is clamped to [0, 1] (except for S == 0, like Unity). </summary>
        public static ValueOutRef HSVToRGB(INodeExporter exporter, MathOperand h, MathOperand s, MathOperand v, MathOperand hdr)
        {
            var m = MathExpression.Float(exporter);

            var h6 = m.Mul(h, 6f);
            var floor = m.Op<Math_FloorNode>(h6);
            var t = m.Sub(h6, floor);
            var var1 = m.Mul(v, m.Sub(1f, s));
            var var2 = m.Mul(v, m.Sub(1f, m.Mul(s, t)));
            var var3 = m.Mul(v, m.Sub(1f, m.Mul(s, m.Sub(1f, t))));

            var toInt = exporter.CreateNode<Type_FloatToIntNode>();
            toInt.ValueIn(Type_FloatToIntNode.IdInputA).ConnectToSource(floor).SetType(TypeRestriction.LimitToFloat);
            var sector = toInt.FirstValueOut().ExpectedType(ExpectedType.Int);

            // Unity's switch over (int)Floor(H * 6), all other sectors stay black
            int[] cases = { 0, 1, 2, 3, 4, 5, 6, -1 };
            ValueOutRef Switch(params MathOperand[] values)
            {
                var node = exporter.CreateNode<Math_SwitchNode>();
                node.Configuration[Math_SwitchNode.IdConfigCases].Value = cases;
                node.ValueIn(Math_SwitchNode.IdSelection).ConnectToSource(sector).SetType(TypeRestriction.LimitToInt);
                node.ValueIn(Math_SwitchNode.IdDefaultValue).SetValue(0f).SetType(TypeRestriction.LimitToFloat);
                for (int i = 0; i < cases.Length; i++)
                    values[i].ConnectTo(node.ValueIn(cases[i].ToString())).SetType(TypeRestriction.LimitToFloat);
                return node.ValueOut(Math_SwitchNode.IdOut).ExpectedType(ExpectedType.Float);
            }

            var r = Switch(v, var2, var1, var1, var3, v, v, v);
            var g = Switch(var3, v, v, var2, var1, var1, var3, var1);
            var b = Switch(var1, var1, var3, v, v, var2, var1, var2);

            ValueOutRef ClampUnlessHdr(ValueOutRef value) => m.Select(hdr, value, m.Op<Math_SaturateNode>(value));

            var rgb = Combine(exporter, ClampUnlessHdr(r), ClampUnlessHdr(g), ClampUnlessHdr(b), 1f);
            var gray = Combine(exporter, v, v, v, 1f);

            var cm = MathExpression.OfType(exporter, UnityMathExpressions.Float4);
            return cm.Select(m.Compare<Math_EqNode>(s, 0f), gray, rgb);
        }

        /// <summary> Color.RGBToHSV </summary>
        public static void RGBToHSV(INodeExporter exporter, MathOperand color, out ValueOutRef h, out ValueOutRef s, out ValueOutRef v)
        {
            var m = MathExpression.Float(exporter);
            Extract(exporter, color, out var r, out var g, out var b, out _);

            // Dominant channel: blue if it's larger than red and green, else green if larger than red, else red
            var blue = m.And(m.Compare<Math_GtNode>(b, g), m.Compare<Math_GtNode>(b, r));
            var green = m.Compare<Math_GtNode>(g, r);
            ValueOutRef Pick(MathOperand ifBlue, MathOperand ifGreen, MathOperand ifRed) => m.Select(blue, ifBlue, m.Select(green, ifGreen, ifRed));

            var offset = Pick(4f, 2f, 0f);
            var dominant = Pick(b, g, r);
            var one = Pick(r, b, g);
            var two = Pick(g, r, b);

            var diff = m.Sub(dominant, m.Min(one, two));
            var notBlack = m.Not(m.Compare<Math_EqNode>(dominant, 0f));
            var hasChroma = m.Not(m.Compare<Math_EqNode>(diff, 0f));

            var oneMinusTwo = m.Sub(one, two);
            var hue = m.Div(m.Add(offset, m.Select(hasChroma, m.Div(oneMinusTwo, diff), oneMinusTwo)), 6f);
            hue = m.Select(m.Compare<Math_LtNode>(hue, 0f), m.Add(hue, 1f), hue);

            v = dominant;
            s = m.Select(notBlack, m.Select(hasChroma, m.Div(diff, dominant), 0f), 0f);
            h = m.Select(notBlack, hue, 0f);
        }
    }

    public static class ColorFunctionUnitExports
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.GammaToLinearSpace), (e, unit, p) =>
                p.Length == 1 ? ColorFunctions.GammaToLinear(e, p[0]) : null);
            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.LinearToGammaSpace), (e, unit, p) =>
                p.Length == 1 ? ColorFunctions.LinearToGamma(e, p[0]) : null);

            GetMemberExpressionUnitExport.Register(typeof(Color), nameof(Color.linear), (e, unit, target) =>
                ColorFunctions.PerChannel(e, target, channel => ColorFunctions.GammaToLinear(e, channel)));
            GetMemberExpressionUnitExport.Register(typeof(Color), nameof(Color.gamma), (e, unit, target) =>
                ColorFunctions.PerChannel(e, target, channel => ColorFunctions.LinearToGamma(e, channel)));
            GetMemberExpressionUnitExport.Register(typeof(Color), nameof(Color.grayscale), (e, unit, target) =>
                ColorFunctions.Grayscale(e, target));
            GetMemberExpressionUnitExport.Register(typeof(Color), nameof(Color.maxColorComponent), (e, unit, target) =>
                ColorFunctions.MaxColorComponent(e, target));

            // HSVToRGB(H, S, V) is HDR (not clamped), like HSVToRGB(H, S, V, true)
            InvokeExpressionUnitExport.Register(typeof(Color), nameof(Color.HSVToRGB), (e, unit, p) =>
            {
                if (p.Length == 3)
                    return ColorFunctions.HSVToRGB(e, p[0], p[1], p[2], true);
                if (p.Length == 4)
                    return ColorFunctions.HSVToRGB(e, p[0], p[1], p[2], p[3]);
                return null;
            });

            InvokeUnitExport.RegisterInvokeExporter(typeof(Color), nameof(Color.RGBToHSV), new ColorRGBToHSVUnitExport());
        }
    }

    /// <summary> Color.RGBToHSV(rgbColor, out H, out S, out V) </summary>
    public class ColorRGBToHSVUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            if (unit.inputParameters.Count != 1)
                return false;

            ColorFunctions.RGBToHSV(unitExporter, unit.inputParameters[0], out var h, out var s, out var v);
            if (unit.valueOutputs.Contains("&H"))
                h.MapToPort(unit.valueOutputs["&H"]);
            if (unit.valueOutputs.Contains("&S"))
                s.MapToPort(unit.valueOutputs["&S"]);
            if (unit.valueOutputs.Contains("&V"))
                v.MapToPort(unit.valueOutputs["&V"]);

            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}
