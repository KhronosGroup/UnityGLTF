using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class ListHelpers
    {

        public static void GetListCount(VariableBasedList list, GltfInteractivityUnitExporterNode.ValueInputSocketData toInputSocket)
        {
            toInputSocket.ConnectToSource(list.getCountNodeSocket);
        }
        
        public static void GetListCount(VariableBasedList list, ValueOutput mapToSocket)
        {
            list.getCountNodeSocket.MapToPort(mapToSocket);
        }

        public static GltfInteractivityUnitExporterNode.ValueOutputSocketData GetListCountSocket(VariableBasedList list)
        {
            return list.getCountNodeSocket;
        }

        public static void ClearList(UnitExporter unitExporter, VariableBasedList list, ControlInput flowIn, ControlOutput flowOut)
        {
            VariablesHelpers.SetVariableStaticValue(unitExporter, list.CountVarId, 0, flowIn, flowOut);
        }

        public static void InsertItem(UnitExporter unitExporter, VariableBasedList list,
            ValueInput atIndexInput, ValueInput valueInput, ControlInput flowIn, ControlOutput flowOut)
        {
            InsertItem(unitExporter, list, out var atIndexInputSocket, out var valueInputSocket, flowIn, flowOut);
            atIndexInputSocket.MapToInputPort(atIndexInput);
            valueInputSocket.MapToInputPort(valueInput);
        }

        public static void InsertItem(UnitExporter unitExporter, VariableBasedList list,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData atIndexInputSocket,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData valueInputSocket,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var addCount = unitExporter.CreateNode(new Math_AddNode());
            addCount.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            addCount.ValueIn("b").SetValue(1);
            addCount.FirstValueOut().ExpectedType(ExpectedType.Int);

            var insertIndexPlusOne = unitExporter.CreateNode(new Math_AddNode());
            atIndexInputSocket = insertIndexPlusOne.ValueIn("a");
            insertIndexPlusOne.ValueIn("b").SetValue(0);
            insertIndexPlusOne.FirstValueOut().ExpectedType(ExpectedType.Int);
            
            FlowHelpers.CreateCustomForLoop(unitExporter, out var startIndex, 
                out var endIndex, out var step, 
                out var forFlowIn, out var currentIndex, 
                out var loopBodyOut, out var completed);

            
            var countMinusOne = unitExporter.CreateNode(new Math_SubNode());
            countMinusOne.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            countMinusOne.ValueIn("b").SetValue(1);
            
            startIndex.ConnectToSource(countMinusOne.FirstValueOut());
            
            endIndex.ConnectToSource(insertIndexPlusOne.FirstValueOut());
            
            step.SetValue(-1);
            // Increase Count
            var setCountVar = VariablesHelpers.SetVariable(unitExporter, list.CountVarId);
            setCountVar.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setCountVar.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(forFlowIn);
            setCountVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(addCount.FirstValueOut());
            
            // Index of item that will be move to current Index
            var indexMinusOne = unitExporter.CreateNode(new Math_SubNode());
            indexMinusOne.ValueIn("a").ConnectToSource(currentIndex);
            indexMinusOne.ValueIn("b").SetValue(1);
            
            // Move existing items 
            GetItem(unitExporter, list, out var getItemIndexInput, out var getItemValueOutput);
            getItemIndexInput.ConnectToSource(indexMinusOne.FirstValueOut());
            
            SetItem(unitExporter, list, out var setItemIndexInput, out var setItemValueInput, out var setItemFlowIn, out var setItemFlowOut);
            setItemIndexInput.ConnectToSource(currentIndex);
            setItemValueInput.ConnectToSource(getItemValueOutput);
            loopBodyOut.ConnectToFlowDestination(setItemFlowIn);

            // Set the new item to index position
            SetItem(unitExporter, list, out var setInsertItemIndexInput, out var setInsertItemValueInput, out var setInsertItemFlowIn, out var setInsertItemFlowOut);
            completed.ConnectToFlowDestination(setInsertItemFlowIn);
            setInsertItemFlowOut.MapToControlOutput(flowOut);
            atIndexInputSocket = atIndexInputSocket.Link(setInsertItemIndexInput);
            valueInputSocket = setInsertItemValueInput;
        }

        public static void AddItem(UnitExporter unitExporter, VariableBasedList list,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData valueInputSocket,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var addCount = unitExporter.CreateNode(new Math_AddNode());
            addCount.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            addCount.ValueIn("b").SetValue(1);
            addCount.FirstValueOut().ExpectedType(ExpectedType.Int);
         
            var setCountVar = VariablesHelpers.SetVariable(unitExporter, list.CountVarId);
            setCountVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(addCount.FirstValueOut());
            setCountVar.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            
            var index = unitExporter.CreateNode(new Math_SubNode());
            index.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            index.ValueIn("b").SetValue(1);
            index.FirstValueOut().ExpectedType(ExpectedType.Int);

            
            SetItem(unitExporter, list, out var indexInput, out valueInputSocket, out var setItemFlowIn, out var setItemFlowOut);
            setCountVar.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(setItemFlowIn);
            setItemFlowOut.MapToControlOutput(flowOut);
            
            indexInput.ConnectToSource(index.FirstValueOut());
        }

        public static void AddItem(UnitExporter unitExporter, VariableBasedList list, ValueInput valueInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            AddItem(unitExporter, list, out var valueInputSocket, flowIn, flowOut);
            valueInputSocket.MapToInputPort(valueInput);
        }

        public static void SetItem(UnitExporter unitExporter, VariableBasedList list,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData indexInput,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData valueInput,
            out GltfInteractivityUnitExporterNode.FlowInSocketData flowIn, out GltfInteractivityUnitExporterNode.FlowOutSocketData flowOut)
        {
            if (list.setValueFlowIn == null)
                CreateSetItemListNodes(unitExporter, list);
            
            var sequence = unitExporter.CreateNode(new Flow_SequenceNode());
            var setIndex = VariablesHelpers.SetVariable(unitExporter, list.CurrentIndexVarId);
            var setValue = VariablesHelpers.SetVariable(unitExporter, list.ValueToSetVarId);

            flowIn = sequence.FlowIn(Flow_SequenceNode.IdFlowIn);
            sequence.FlowOut("0").ConnectToFlowDestination(setIndex.FlowIn(Variable_SetNode.IdFlowIn));
            flowOut = sequence.FlowOut("1");
            
            setIndex.FlowOut(Variable_SetNode.IdFlowOut)
                .ConnectToFlowDestination(setValue.FlowIn(Variable_SetNode.IdFlowIn));
            
            setValue.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(list.setValueFlowIn);

            indexInput = setIndex.ValueIn(Variable_SetNode.IdInputValue);
            valueInput = setValue.ValueIn(Variable_SetNode.IdInputValue);      
        }

        public static void SetItem(UnitExporter unitExporter, VariableBasedList list,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData indexInput,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData valueInput,
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
            GltfInteractivityUnitExporterNode.ValueOutputSocketData indexInput, out GltfInteractivityUnitExporterNode.ValueOutputSocketData valueOutput)
        {
            GetItem(unitExporter, list, out var indexInputSocket, out valueOutput);
            indexInputSocket.ConnectToSource(indexInput);
        }
        
        public static void GetItem(UnitExporter unitExporter, VariableBasedList list,
            ValueInput indexInput, out GltfInteractivityUnitExporterNode.ValueOutputSocketData valueOutput)
        {
            GetItem(unitExporter, list, out var indexInputSocket, out valueOutput);
            indexInputSocket.MapToInputPort(indexInput);
        }

        public static void GetItem(UnitExporter unitExporter, VariableBasedList list, out GltfInteractivityUnitExporterNode.ValueInputSocketData indexInput, out GltfInteractivityUnitExporterNode.ValueOutputSocketData valueOutput)
        {
            var varType = unitExporter.exportContext.variables[list.StartIndex].Type;
           
            var switchNode = unitExporter.CreateNode(new Math_SwitchNode());
            indexInput = switchNode.ValueIn(Math_SwitchNode.IdSelection).SetType(TypeRestriction.LimitToInt);
            valueOutput = switchNode.ValueOut(Math_SwitchNode.IdOut).ExpectedType(ExpectedType.GtlfType(varType));
            
            int[] cases = new int[list.Capacity];
            
            int index = 0;
            for (int i = list.StartIndex; i <= list.EndIndex; i++)
            {
                cases[index] = index;
                VariablesHelpers.GetVariable(unitExporter, i, out var valueOut);
                switchNode.ValueIn(index.ToString()).ConnectToSource(valueOut).SetType(TypeRestriction.LimitToType(varType));

                index++;
            }
            VariablesHelpers.GetVariable(unitExporter, list.StartIndex, out var firstValue);
            switchNode.ValueIn(Math_SwitchNode.IdDefaultValue).ConnectToSource(firstValue).SetType(TypeRestriction.LimitToType(varType));
            
            switchNode.Configuration[Math_SwitchNode.IdConfigCases].Value = cases;
        }

        public static void RemoveListItemAt(UnitExporter unitExporter, VariableBasedList list, ValueInput index, ControlInput flowIn, ControlOutput flowOut)
        {
            RemoveListItemAt(unitExporter, list, out var indexSocket, out var flowInSocket, out var flowOutSocket);
            indexSocket.MapToInputPort(index);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
        }

        public static void RemoveListItemAt(UnitExporter unitExporter, VariableBasedList list,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData index, out GltfInteractivityUnitExporterNode.FlowInSocketData flowIn, out GltfInteractivityUnitExporterNode.FlowOutSocketData flowOut)
        {
            var countMinusOne = unitExporter.CreateNode(new Math_SubNode());
            countMinusOne.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            countMinusOne.ValueIn("b").SetValue(1);
            countMinusOne.FirstValueOut().ExpectedType(ExpectedType.Int);

            var forLoop = unitExporter.CreateNode(new Flow_ForLoopNode());
            index = forLoop.ValueIn(Flow_ForLoopNode.IdStartIndex);
            forLoop.ValueIn(Flow_ForLoopNode.IdEndIndex).ConnectToSource(countMinusOne.FirstValueOut());
            forLoop.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
            flowIn = forLoop.FlowIn(Flow_ForLoopNode.IdFlowIn);
            
            var currentIndexPlusOne = unitExporter.CreateNode(new Math_AddNode());
            currentIndexPlusOne.ValueIn("a").ConnectToSource(forLoop.ValueOut(Flow_ForLoopNode.IdIndex));
            currentIndexPlusOne.ValueIn("b").SetValue(1);
            currentIndexPlusOne.FirstValueOut().ExpectedType(ExpectedType.Int);
            
            // Move existing items 
            GetItem(unitExporter, list, out var getItemIndexInput, out var getItemValueOutput);
            getItemIndexInput.ConnectToSource(currentIndexPlusOne.FirstValueOut());
            
            SetItem(unitExporter, list, out var setItemIndexInput, out var setItemValueInput, out var setItemFlowIn, out var setItemFlowOut);
            setItemIndexInput.ConnectToSource(forLoop.ValueOut(Flow_ForLoopNode.IdIndex));
            setItemValueInput.ConnectToSource(getItemValueOutput);
            forLoop.FlowOut(Flow_ForLoopNode.IdLoopBody).ConnectToFlowDestination(setItemFlowIn);
            
            // Set new Count
            var setCountVar = VariablesHelpers.SetVariable(unitExporter, list.CountVarId);
            setCountVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(countMinusOne.FirstValueOut());
            forLoop.FlowOut(Flow_ForLoopNode.IdCompleted).ConnectToFlowDestination(setCountVar.FlowIn(Variable_SetNode.IdFlowIn));

            flowOut = setCountVar.FlowOut(Variable_SetNode.IdFlowOut);
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

        private static void CreateSetItemListNodes(UnitExporter unitExporter, VariableBasedList list)
        {
            VariablesHelpers.GetVariable(unitExporter, list.CurrentIndexVarId, out var currentIndexValueOut);
            VariablesHelpers.GetVariable(unitExporter, list.ValueToSetVarId, out var valueToSetValueOut);
            // Set Values
            int[] indices = new int[list.Capacity];
            for (int i = 0; i < list.Capacity; i++)
                indices[i] = i;
            
            var flowSwitch = unitExporter.CreateNode(new Flow_SwitchNode());
            list.setValueFlowIn = flowSwitch.FlowIn(Flow_SwitchNode.IdFlowIn);
            flowSwitch.ValueIn(Flow_SwitchNode.IdSelection).ConnectToSource(currentIndexValueOut);
            flowSwitch.Configuration["cases"] = new GltfInteractivityNode.ConfigData { Value = indices };

            for (int i = 0; i < indices.Length; i++)
            {
                flowSwitch.FlowConnections.Add(i.ToString(), new GltfInteractivityNode.FlowSocketData());
                
                VariablesHelpers.SetVariable(unitExporter, list.StartIndex + i, valueToSetValueOut, flowSwitch.FlowOut(i.ToString()), null);
            }
        }
        
        public static void CreateListNodes(UnitExporter unitExporter, VariableBasedList list)
        {
            VariablesHelpers.GetVariable(unitExporter, list.CountVarId, out var listCountValueOut);
            list.getCountNodeSocket = listCountValueOut;
        }
    }
}