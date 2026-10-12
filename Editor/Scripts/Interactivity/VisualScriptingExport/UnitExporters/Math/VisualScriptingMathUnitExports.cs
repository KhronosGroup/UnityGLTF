using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Exports a Visual Scripting unit with a builder function.
    /// </summary>
    public class UnitExpressionExport<TUnit> : IUnitExporter where TUnit : class, IUnit
    {
        public delegate bool Builder(UnitExporter unitExporter, TUnit unit);

        public Type unitType { get => typeof(TUnit); }

        private readonly Builder builder;

        public UnitExpressionExport(Builder builder)
        {
            this.builder = builder;
        }

        public static void Register(Builder builder)
        {
            UnitExporterRegistry.RegisterExporter(new UnitExpressionExport<TUnit>(builder));
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            return builder(unitExporter, unitExporter.unit as TUnit);
        }
    }

    /// <summary>
    /// Exporters for the Visual Scripting math units (Scalar, Vector2, Vector3, Vector4) which need more than one glTF node.
    /// </summary>
    public static class VisualScriptingMathUnitExports
    {
        private static int TypeIndex(Type type) => GltfTypes.TypeIndex(type);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            RegisterRound();
            RegisterAverage();
            RegisterPerSecond();

            UnitExpressionExport<ScalarExponentiate>.Register((e, unit) =>
            {
                MathExpression.Float(e).Op<Math_PowNode>(unit.@base, unit.exponent).MapToPort(unit.power);
                return true;
            });

            UnitExpressionExport<ScalarRoot>.Register((e, unit) =>
            {
                var m = MathExpression.Float(e);
                m.Op<Math_PowNode>(unit.radicand, m.Div(1f, unit.degree)).MapToPort(unit.root);
                return true;
            });

            // input / |input|, 0 for 0: the same as math/sign
            UnitExpressionExport<ScalarNormalize>.Register((e, unit) =>
            {
                MathExpression.Float(e).Op<Math_SignNode>(unit.input).MapToPort(unit.output);
                return true;
            });

            UnitExpressionExport<ScalarMoveTowards>.Register((e, unit) =>
            {
                var maxDelta = PerSecondIfNeeded(e, unit.maxDelta, unit.perSecond);
                UnityMathExpressions.MoveTowards(e, unit.current, unit.target, maxDelta).MapToPort(unit.result);
                return true;
            });

            UnitExpressionExport<Vector2Angle>.Register((e, unit) =>
            {
                UnityMathExpressions.Angle(e, TypeIndex(typeof(Vector2)), unit.a, unit.b).MapToPort(unit.angle);
                return true;
            });

            UnitExpressionExport<Vector3Angle>.Register((e, unit) =>
            {
                UnityMathExpressions.Angle(e, TypeIndex(typeof(Vector3)), unit.a, unit.b).MapToPort(unit.angle);
                return true;
            });

            UnitExpressionExport<Vector3Project>.Register((e, unit) =>
            {
                UnityMathExpressions.Project(e, TypeIndex(typeof(Vector3)), unit.a, unit.b).MapToPort(unit.projection);
                return true;
            });

            // Visual Scripting's Vector2 Project: Dot(a, b) * b.normalized
            UnitExpressionExport<Vector2Project>.Register((e, unit) =>
            {
                var float2 = TypeIndex(typeof(Vector2));
                var vm = MathExpression.OfType(e, float2);
                var dot = vm.Dot(unit.a, unit.b);
                vm.Mul(UnityMathExpressions.Normalize(e, float2, unit.b), UnityMathExpressions.Splat(e, float2, dot)).MapToPort(unit.projection);
                return true;
            });
        }

        private static MathOperand PerSecondIfNeeded(UnitExporter unitExporter, ValueInput value, bool perSecond)
        {
            if (!perSecond)
                return value;

            TimeHelpers.AddTickNode(unitExporter, TimeHelpers.GetTimeValueOption.DeltaTime, out var deltaTime);
            return MathExpression.Float(unitExporter).Mul(value, deltaTime);
        }

        private static void RegisterRound()
        {
            ValueOutRef BuildRound<TInput, TOutput>(UnitExporter e, Round<TInput, TOutput> unit, int gltfType)
            {
                var m = MathExpression.OfType(e, gltfType);
                switch (unit.rounding)
                {
                    case Round<TInput, TOutput>.Rounding.Floor:
                        return m.Op<Math_FloorNode>(unit.input);
                    case Round<TInput, TOutput>.Rounding.Ceiling:
                        return m.Op<Math_CeilNode>(unit.input);
                    default:
                        // "Away From Zero" uses Mathf.Round / Mathf.RoundToInt, which round half-way values to even
                        return UnityMathExpressions.RoundHalfToEven(e, gltfType, unit.input);
                }
            }

            UnitExpressionExport<ScalarRound>.Register((e, unit) =>
            {
                var rounded = BuildRound(e, unit, TypeIndex(typeof(float)));
                var toInt = e.CreateNode<Type_FloatToIntNode>();
                toInt.ValueIn(Type_FloatToIntNode.IdInputA).ConnectToSource(rounded);
                toInt.FirstValueOut().ExpectedType(ExpectedType.Int).MapToPort(unit.output);
                return true;
            });

            UnitExpressionExport<Vector2Round>.Register((e, unit) =>
            {
                BuildRound(e, unit, TypeIndex(typeof(Vector2))).MapToPort(unit.output);
                return true;
            });

            UnitExpressionExport<Vector3Round>.Register((e, unit) =>
            {
                BuildRound(e, unit, TypeIndex(typeof(Vector3))).MapToPort(unit.output);
                return true;
            });

            UnitExpressionExport<Vector4Round>.Register((e, unit) =>
            {
                BuildRound(e, unit, TypeIndex(typeof(Vector4))).MapToPort(unit.output);
                return true;
            });
        }

        private static void RegisterAverage()
        {
            bool BuildAverage<T>(UnitExporter e, Average<T> unit)
            {
                var operands = unit.multiInputs.Select(input => (MathOperand)input).ToArray();
                UnityMathExpressions.Average(e, TypeIndex(typeof(T)), operands).MapToPort(unit.average);
                return true;
            }

            UnitExpressionExport<ScalarAverage>.Register((e, unit) => BuildAverage(e, unit));
            UnitExpressionExport<Vector2Average>.Register((e, unit) => BuildAverage(e, unit));
            UnitExpressionExport<Vector3Average>.Register((e, unit) => BuildAverage(e, unit));
            UnitExpressionExport<Vector4Average>.Register((e, unit) => BuildAverage(e, unit));
        }

        private static void RegisterPerSecond()
        {
            // input * Time.deltaTime
            bool BuildPerSecond<T>(UnitExporter e, PerSecond<T> unit)
            {
                var gltfType = TypeIndex(typeof(T));
                TimeHelpers.AddTickNode(e, TimeHelpers.GetTimeValueOption.DeltaTime, out var deltaTime);
                var m = MathExpression.OfType(e, gltfType);
                m.Mul(unit.input, UnityMathExpressions.Splat(e, gltfType, deltaTime)).MapToPort(unit.output);
                return true;
            }

            UnitExpressionExport<ScalarPerSecond>.Register((e, unit) => BuildPerSecond(e, unit));
            UnitExpressionExport<Vector2PerSecond>.Register((e, unit) => BuildPerSecond(e, unit));
            UnitExpressionExport<Vector3PerSecond>.Register((e, unit) => BuildPerSecond(e, unit));
            UnitExpressionExport<Vector4PerSecond>.Register((e, unit) => BuildPerSecond(e, unit));
        }
    }
}
