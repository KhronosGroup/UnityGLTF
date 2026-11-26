using System;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Exports an InvokeMember unit whose result can be built as a pure math expression of its parameters.
    /// The builder gets the parameter inputs in declaration order and returns the result, or null when the overload is not supported.
    /// </summary>
    public class InvokeExpressionUnitExport : IUnitExporter
    {
        public delegate ValueOutRef Builder(UnitExporter unitExporter, InvokeMember unit, ValueInput[] parameters);

        public Type unitType { get => typeof(InvokeMember); }

        private readonly Builder builder;

        public InvokeExpressionUnitExport(Builder builder)
        {
            this.builder = builder;
        }

        public static void Register(Type type, string methodName, Builder builder)
        {
            InvokeUnitExport.RegisterInvokeExporter(type, methodName, new InvokeExpressionUnitExport(builder));
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            if (unit.result == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Only the overload with a return value is supported.");
                return false;
            }

            var parameters = new ValueInput[unit.inputParameters.Count];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = unit.inputParameters[i];

            var result = builder(unitExporter, unit, parameters);
            if (result == null)
            {
                UnitExportLogging.AddErrorLog(unit, $"This overload of {unit.member.name} is not supported.");
                return false;
            }

            result.MapToPort(unit.result);
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }

    /// <summary>
    /// Exports a GetMember unit (instance property or field) whose value can be built as a math expression of the target.
    /// </summary>
    public class GetMemberExpressionUnitExport : IUnitExporter
    {
        public delegate ValueOutRef Builder(UnitExporter unitExporter, GetMember unit, ValueInput target);

        public Type unitType { get => typeof(GetMember); }

        private readonly Builder builder;

        public GetMemberExpressionUnitExport(Builder builder)
        {
            this.builder = builder;
        }

        public static void Register(Type type, string memberName, Builder builder)
        {
            GetMemberUnitExport.RegisterMemberExporter(type, memberName, new GetMemberExpressionUnitExport(builder));
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            var result = builder(unitExporter, unit, unit.target);
            if (result == null)
                return false;

            result.MapToPort(unit.value);
            return true;
        }
    }
}
