using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class ListHelpers
    {
        public static void GetListCount(VariableBasedList list, ValueInRef toInputSocket)
        {
            if (list.getCountNodeSocket == null)
                return;
            toInputSocket.ConnectToSource(list.getCountNodeSocket);
        }

        public static ValueOutRef GetListCountSocket(VariableBasedList list)
        {
            return list.getCountNodeSocket;
        }

        public static void ClearList(INodeExporter exporter, VariableBasedList list, out FlowInRef flowIn,
            out FlowOutRef flowOut)
        {
            VariablesHelpers.SetVariableStaticValue(exporter, list.CountVarId, 0, out flowIn, out flowOut);
        }

        public static void GetItem(INodeExporter exporter, VariableBasedList list, out ValueInRef indexInput,
            out ValueOutRef valueOutput)
        {
            var varType = exporter.Context.variables[list.StartIndex].Type;

            var switchNode = exporter.CreateNode<Math_SwitchNode>();
            indexInput = switchNode.ValueIn(Math_SwitchNode.IdSelection).SetType(TypeRestriction.LimitToInt);
            valueOutput = switchNode.ValueOut(Math_SwitchNode.IdOut).ExpectedType(ExpectedType.GtlfType(varType));

            int[] cases = new int[list.Capacity];

            int index = 0;
            for (int i = list.StartIndex; i <= list.EndIndex; i++)
            {
                cases[index] = index;
                VariablesHelpers.GetVariable(exporter, i, out var valueOut);
                switchNode.ValueIn(index.ToString()).ConnectToSource(valueOut)
                    .SetType(TypeRestriction.LimitToType(varType));

                index++;
            }

            VariablesHelpers.GetVariable(exporter, list.StartIndex, out var firstValue);
            switchNode.ValueIn(Math_SwitchNode.IdDefaultValue).ConnectToSource(firstValue)
                .SetType(TypeRestriction.LimitToType(varType));

            switchNode.Configuration[Math_SwitchNode.IdConfigCases].Value = cases;
        }

        public static void InsertItem(INodeExporter exporter, VariableBasedList list,
            out ValueInRef atIndexInputSocket,
            out ValueInRef valueInputSocket,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var addCount = exporter.CreateNode<Math_AddNode>();
            addCount.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            addCount.ValueIn("b").SetValue(1);
            addCount.FirstValueOut().ExpectedType(ExpectedType.Int);

            var insertIndexPlusOne = exporter.CreateNode<Math_AddNode>();
            atIndexInputSocket = insertIndexPlusOne.ValueIn("a");
            insertIndexPlusOne.ValueIn("b").SetValue(0);
            insertIndexPlusOne.FirstValueOut().ExpectedType(ExpectedType.Int);

            FlowHelpers.CreateCustomForLoop(exporter, out var startIndex,
                out var endIndex, out var step,
                out var forFlowIn, out var currentIndex,
                out var loopBodyOut, out var completed);

            var countMinusOne = exporter.CreateNode<Math_SubNode>();
            countMinusOne.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            countMinusOne.ValueIn("b").SetValue(1);

            startIndex.ConnectToSource(countMinusOne.FirstValueOut());

            endIndex.ConnectToSource(insertIndexPlusOne.FirstValueOut());

            step.SetValue(-1);
            // Increase Count
            var setCountVar = VariablesHelpers.SetVariable(exporter, list.CountVarId);
            flowIn = setCountVar.FlowIn(Variable_SetNode.IdFlowIn);
            setCountVar.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(forFlowIn);
            setCountVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(addCount.FirstValueOut());

            // Index of item that will be move to current Index
            var indexMinusOne = exporter.CreateNode<Math_SubNode>();
            indexMinusOne.ValueIn("a").ConnectToSource(currentIndex);
            indexMinusOne.ValueIn("b").SetValue(1);

            // Move existing items 
            GetItem(exporter, list, out var getItemIndexInput, out var getItemValueOutput);
            getItemIndexInput.ConnectToSource(indexMinusOne.FirstValueOut());

            SetItem(exporter, list, out var setItemIndexInput, out var setItemValueInput, out var setItemFlowIn,
                out var setItemFlowOut);
            setItemIndexInput.ConnectToSource(currentIndex);
            setItemValueInput.ConnectToSource(getItemValueOutput);
            loopBodyOut.ConnectToFlowDestination(setItemFlowIn);

            // Set the new item to index position
            SetItem(exporter, list, out var setInsertItemIndexInput, out var setInsertItemValueInput,
                out var setInsertItemFlowIn, out var setInsertItemFlowOut);
            completed.ConnectToFlowDestination(setInsertItemFlowIn);
            flowOut = setInsertItemFlowOut;
            atIndexInputSocket = atIndexInputSocket.Link(setInsertItemIndexInput);
            valueInputSocket = setInsertItemValueInput;
        }

        public static void AddItem(INodeExporter exporter, VariableBasedList list,
            out ValueInRef valueInputSocket,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var addCount = exporter.CreateNode<Math_AddNode>();
            addCount.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            addCount.ValueIn("b").SetValue(1);
            addCount.FirstValueOut().ExpectedType(ExpectedType.Int);

            var setCountVar = VariablesHelpers.SetVariable(exporter, list.CountVarId);
            setCountVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(addCount.FirstValueOut());
            flowIn = setCountVar.FlowIn(Variable_SetNode.IdFlowIn);

            var index = exporter.CreateNode<Math_SubNode>();
            index.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            index.ValueIn("b").SetValue(1);
            index.FirstValueOut().ExpectedType(ExpectedType.Int);


            SetItem(exporter, list, out var indexInput, out valueInputSocket, out var setItemFlowIn,
                out var setItemFlowOut);
            setCountVar.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(setItemFlowIn);
            flowOut = setItemFlowOut;

            indexInput.ConnectToSource(index.FirstValueOut());
        }

        public static void SetItem(INodeExporter exporter, VariableBasedList list,
            out ValueInRef indexInput,
            out ValueInRef valueInput,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            if (list.setValueFlowIn == null)
                CreateSetItemListNodes(exporter, list);

            var sequence = exporter.CreateNode<Flow_SequenceNode>();
            var setIndex = VariablesHelpers.SetVariable(exporter, list.CurrentIndexVarId);
            var setValue = VariablesHelpers.SetVariable(exporter, list.ValueToSetVarId);

            flowIn = sequence.FlowIn(Flow_SequenceNode.IdFlowIn);
            sequence.FlowOut("0").ConnectToFlowDestination(setIndex.FlowIn(Variable_SetNode.IdFlowIn));
            flowOut = sequence.FlowOut("1");

            setIndex.FlowOut(Variable_SetNode.IdFlowOut)
                .ConnectToFlowDestination(setValue.FlowIn(Variable_SetNode.IdFlowIn));

            setValue.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(list.setValueFlowIn);

            indexInput = setIndex.ValueIn(Variable_SetNode.IdInputValue);
            valueInput = setValue.ValueIn(Variable_SetNode.IdInputValue);
        }

        private static void CreateSetItemListNodes(INodeExporter exporter, VariableBasedList list)
        {
            VariablesHelpers.GetVariable(exporter, list.CurrentIndexVarId, out var currentIndexValueOut);
            VariablesHelpers.GetVariable(exporter, list.ValueToSetVarId, out var valueToSetValueOut);
            // Set Values
            int[] indices = new int[list.Capacity];
            for (int i = 0; i < list.Capacity; i++)
                indices[i] = i;

            var flowSwitch = exporter.CreateNode<Flow_SwitchNode>();
            list.setValueFlowIn = flowSwitch.FlowIn(Flow_SwitchNode.IdFlowIn);
            flowSwitch.ValueIn(Flow_SwitchNode.IdSelection).ConnectToSource(currentIndexValueOut);
            flowSwitch.Configuration["cases"] = new GltfInteractivityNode.ConfigData { Value = indices };

            for (int i = 0; i < indices.Length; i++)
            {
                flowSwitch.FlowConnections.Add(i.ToString(), new GltfInteractivityNode.FlowSocketData());

                VariablesHelpers.SetVariable(exporter, list.StartIndex + i, valueToSetValueOut,
                    flowSwitch.FlowOut(i.ToString()), null);
            }
        }

        public static void RemoveListItemAt(INodeExporter exporter, VariableBasedList list,
            out ValueInRef index, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var countMinusOne = exporter.CreateNode<Math_SubNode>();
            countMinusOne.ValueIn("a").ConnectToSource(GetListCountSocket(list));
            countMinusOne.ValueIn("b").SetValue(1);
            countMinusOne.FirstValueOut().ExpectedType(ExpectedType.Int);

            var forLoop = exporter.CreateNode<Flow_ForLoopNode>();
            index = forLoop.ValueIn(Flow_ForLoopNode.IdStartIndex);
            forLoop.ValueIn(Flow_ForLoopNode.IdEndIndex).ConnectToSource(countMinusOne.FirstValueOut());
            forLoop.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
            flowIn = forLoop.FlowIn(Flow_ForLoopNode.IdFlowIn);

            var currentIndexPlusOne = exporter.CreateNode<Math_AddNode>();
            currentIndexPlusOne.ValueIn("a").ConnectToSource(forLoop.ValueOut(Flow_ForLoopNode.IdIndex));
            currentIndexPlusOne.ValueIn("b").SetValue(1);
            currentIndexPlusOne.FirstValueOut().ExpectedType(ExpectedType.Int);

            // Move existing items 
            GetItem(exporter, list, out var getItemIndexInput, out var getItemValueOutput);
            getItemIndexInput.ConnectToSource(currentIndexPlusOne.FirstValueOut());

            SetItem(exporter, list, out var setItemIndexInput, out var setItemValueInput, out var setItemFlowIn,
                out var setItemFlowOut);
            setItemIndexInput.ConnectToSource(forLoop.ValueOut(Flow_ForLoopNode.IdIndex));
            setItemValueInput.ConnectToSource(getItemValueOutput);
            forLoop.FlowOut(Flow_ForLoopNode.IdLoopBody).ConnectToFlowDestination(setItemFlowIn);

            // Set new Count
            var setCountVar = VariablesHelpers.SetVariable(exporter, list.CountVarId);
            setCountVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(countMinusOne.FirstValueOut());
            forLoop.FlowOut(Flow_ForLoopNode.IdCompleted)
                .ConnectToFlowDestination(setCountVar.FlowIn(Variable_SetNode.IdFlowIn));

            flowOut = setCountVar.FlowOut(Variable_SetNode.IdFlowOut);
        }

        public static void CreateListNodes(INodeExporter exporter, VariableBasedList list)
        {
            VariablesHelpers.GetVariable(exporter, list.CountVarId, out var listCountValueOut);
            list.getCountNodeSocket = listCountValueOut;
        }
    }
}