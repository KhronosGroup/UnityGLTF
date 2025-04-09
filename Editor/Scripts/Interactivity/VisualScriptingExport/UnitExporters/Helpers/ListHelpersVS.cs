using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ListHelpersVS : ListHelpers
    {
        public static void GetListCount(VariableBasedList list, ValueOutput mapToSocket)
        {
            if (list.getCountNodeSocket == null)
                return;
            
            list.getCountNodeSocket.MapToPort(mapToSocket);
        }
        
        public static void ClearList(INodeExporter exporter, VariableBasedList list, ControlInput flowIn, ControlOutput flowOut)
        {
            VariablesHelpersVS.SetVariableStaticValue(exporter, list.CountVarId, 0, flowIn, flowOut);
        }
        
        public static void InsertItem(UnitExporter unitExporter, VariableBasedList list,
            ValueInput atIndexInput, ValueInput valueInput, ControlInput flowIn, ControlOutput flowOut)
        {
            InsertItem(unitExporter, list, out var atIndexInputSocket, out var valueInputSocket, flowIn, flowOut);
            atIndexInputSocket.MapToInputPort(atIndexInput);
            valueInputSocket.MapToInputPort(valueInput);
        }

        public static void InsertItem(UnitExporter unitExporter, VariableBasedList list,
            out ValueInRef atIndexInputSocket,
            out ValueInRef valueInputSocket,
            ControlInput flowIn, ControlOutput flowOut)
        {
            InsertItem(unitExporter, list, out atIndexInputSocket, out valueInputSocket, out var flowInSocket, out var flowOutSocket);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
        }

        public static void AddItem(UnitExporter unitExporter, VariableBasedList list,
            out ValueInRef valueInputSocket,
            ControlInput flowIn, ControlOutput flowOut)
        {
            AddItem(unitExporter, list, out valueInputSocket, out var flowInSocket, out var flowOutSocket);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
        }
        
        public static void AddItem(UnitExporter unitExporter, VariableBasedList list, ValueInput valueInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            AddItem(unitExporter, list, out var valueInputSocket, flowIn, flowOut);
            valueInputSocket.MapToInputPort(valueInput);
        }
        
        public static void SetItem(UnitExporter unitExporter, VariableBasedList list,
            out ValueInRef indexInput,
            out ValueInRef valueInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            SetItem(unitExporter, list, out indexInput, out valueInput, out var flowInSocket, out var flowOutSocket);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
        }
        
        public static void SetItem(UnitExporter unitExporter, VariableBasedList list, ValueInput indexInput, ValueInput valueInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            SetItem(unitExporter, list, out var indexInputSocket, out var valueInputSocket, flowIn, flowOut);
            indexInputSocket.MapToInputPort(indexInput);
            valueInputSocket.MapToInputPort(valueInput);
        }
        
        public static void GetItem(UnitExporter unitExporter, VariableBasedList list,
            ValueOutRef indexInput, out ValueOutRef valueOutput)
        {
            GetItem(unitExporter, list, out var indexInputSocket, out valueOutput);
            indexInputSocket.ConnectToSource(indexInput);
        }
        
        public static void GetItem(UnitExporter unitExporter, VariableBasedList list,
            ValueInput indexInput, out ValueOutRef valueOutput)
        {
            GetItem(unitExporter, list, out var indexInputSocket, out valueOutput);
            indexInputSocket.MapToInputPort(indexInput);
        }
        
        public static void RemoveListItemAt(UnitExporter unitExporter, VariableBasedList list, ValueInput index, ControlInput flowIn, ControlOutput flowOut)
        {
            RemoveListItemAt(unitExporter, list, out var indexSocket, out var flowInSocket, out var flowOutSocket);
            indexSocket.MapToInputPort(index);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
        }
        
        public static VariableBasedList FindListByConnections(VisualScriptingExportContext context, IUnit unit)
        {
            var l = context.GetListByCreator(unit);
            if (l != null)
                return l;

            if (unit is GetVariable getVarUnit)
            {
                var varDeclaration = context.GetVariableDeclaration(getVarUnit);
                if (varDeclaration != null)
                {
                    var varUnitList = context.GetListByCreator(varDeclaration);
                    if (varUnitList != null)
                        return varUnitList;
                }
            }

            foreach (var input in unit.valueInputs)
            {
                if (input.type != typeof(string) && ((input.type.IsInterface && input.type == typeof(IEnumerable)) || input.type.GetInterfaces().Contains(typeof(IEnumerable))))
                {
                    foreach (var c in input.validConnections)
                    {
                        var varList = FindListByConnections(context, c.source.unit);
                        if (varList != null)
                            return varList;
                    }
                }
            }

            return null;
        }


    }
}