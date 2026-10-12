using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// glTF loops can't be interrupted, so a Break sets a "broken" variable of its loop, which the loop checks before every iteration.
    /// The loop exporters (For, While, For Each) use <see cref="GetBreakVariable"/> to add the check when their body contains a Break.
    /// </summary>
    public static class LoopBreakHelper
    {
        private static bool IsLoop(IUnit unit) => unit is For || unit is While || unit is ForEach;

        private static ControlOutput GetBody(IUnit loop)
        {
            switch (loop)
            {
                case For forLoop: return forLoop.body;
                case While whileLoop: return whileLoop.body;
                case ForEach forEach: return forEach.body;
                default: return null;
            }
        }

        private static ControlOutput GetExit(IUnit loop)
        {
            switch (loop)
            {
                case For forLoop: return forLoop.exit;
                case While whileLoop: return whileLoop.exit;
                case ForEach forEach: return forEach.exit;
                default: return null;
            }
        }

        /// <summary>
        /// All Break units which break this loop: reachable from the loop body, without the bodies of nested loops.
        /// </summary>
        public static List<Break> FindBreaks(IUnit loop)
        {
            var breaks = new List<Break>();
            var body = GetBody(loop);
            if (body == null)
                return breaks;

            var visited = new HashSet<IUnit> { loop };
            var stack = new Stack<ControlOutput>();
            stack.Push(body);
            while (stack.Count > 0)
            {
                var output = stack.Pop();
                if (!output.hasValidConnection)
                    continue;

                var unit = output.connection.destination.unit;
                if (!visited.Add(unit))
                    continue;

                if (unit is Break breakUnit)
                {
                    breaks.Add(breakUnit);
                    continue;
                }

                if (IsLoop(unit))
                {
                    // A Break in a nested loop only breaks the nested loop
                    stack.Push(GetExit(unit));
                    continue;
                }

                foreach (var next in unit.controlOutputs)
                    stack.Push(next);
            }

            return breaks;
        }

        public static IUnit FindLoop(Break breakUnit)
        {
            foreach (var unit in breakUnit.graph.units)
            {
                if (IsLoop(unit) && FindBreaks(unit).Contains(breakUnit))
                    return unit;
            }
            return null;
        }

        private static int AddBreakVariable(UnitExporter unitExporter, IUnit loop)
        {
            return unitExporter.vsExportContext.AddVariableWithIdIfNeeded($"loopBreak_{loop.guid}", false, VariableKind.Graph, typeof(bool));
        }

        /// <summary>
        /// Returns the break variable of the loop, or -1 when the loop body contains no Break.
        /// </summary>
        public static int GetBreakVariable(UnitExporter unitExporter, IUnit loop)
        {
            if (FindBreaks(loop).Count == 0)
                return -1;
            return AddBreakVariable(unitExporter, loop);
        }

        public static int GetBreakVariable(UnitExporter unitExporter, Break breakUnit, out IUnit loop)
        {
            loop = FindLoop(breakUnit);
            if (loop == null)
                return -1;
            return AddBreakVariable(unitExporter, loop);
        }

        /// <summary>
        /// condition AND NOT broken
        /// </summary>
        public static ValueOutRef AndNotBroken(UnitExporter unitExporter, int breakVariable, MathOperand condition)
        {
            VariablesHelpers.GetVariable(unitExporter, breakVariable, out var broken);
            var m = new MathExpression(unitExporter);
            return m.And(condition, m.Not(broken));
        }

        /// <summary>
        /// Resets the break variable, e.g. before a loop starts.
        /// </summary>
        public static GltfInteractivityExportNode ResetBreak(UnitExporter unitExporter, int breakVariable, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            return VariablesHelpers.SetVariableStaticValue(unitExporter, breakVariable, false, out flowIn, out flowOut);
        }
    }

    public class BreakUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(Break); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new BreakUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Break;
            var breakVariable = LoopBreakHelper.GetBreakVariable(unitExporter, unit, out _);
            if (breakVariable == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Break is not inside the body of a For, While or For Each loop.");
                return false;
            }

            VariablesHelpers.SetVariableStaticValue(unitExporter, breakVariable, true, out var flowIn, out _);
            flowIn.MapToControlInput(unit.enter);
            return true;
        }
    }
}
