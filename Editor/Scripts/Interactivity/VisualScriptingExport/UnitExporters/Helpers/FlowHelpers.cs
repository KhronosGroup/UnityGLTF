using System.Collections.Generic;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class FlowHelpers
    {
        public static bool RequiresCoroutines(ControlInput input, out ControlInput coroutineControlInput)
        {
            coroutineControlInput = null;
            var visited = new HashSet<IUnit>();
            var stack = new Stack<ControlInput>();
            stack.Push(input);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (visited.Contains(current.unit))
                    continue;

                visited.Add(current.unit);

                if (current.requiresCoroutine)
                {
                    coroutineControlInput = current;
                    return true;

                }

                foreach (var controlOutput in current.unit.controlOutputs)
                {
                    if (!controlOutput.hasValidConnection)
                        continue;

                    stack.Push(controlOutput.connection.destination);
                }
            }

            return false;
        }
        
         public static void CreateCustomForLoopWithFlowStep(UnitExporter unitExporter, 
            out GltfInteractivityUnitExporterNode.ValueInputSocketData startIndex,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData endIndex,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData step,
            out GltfInteractivityUnitExporterNode.FlowInSocketData flowIn,
            out GltfInteractivityUnitExporterNode.FlowInSocketData nextStepIn,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData currentIndex,
            out GltfInteractivityUnitExporterNode.FlowOutSocketData loopBodyOut,
            out GltfInteractivityUnitExporterNode.FlowOutSocketData completed)
        {
            var indexVar = unitExporter.exportContext.AddVariableWithIdIfNeeded("ForLoopIndex"+System.Guid.NewGuid().ToString(), 0, VariableKind.Scene, typeof(int));
            
            var startSequ = unitExporter.CreateNode(new Flow_SequenceNode());
            flowIn = startSequ.FlowIn(Flow_SequenceNode.IdFlowIn);
            
            var setStartIndexVar = VariablesHelpers.SetVariable(unitExporter, indexVar);
            var branch  = unitExporter.CreateNode(new Flow_BranchNode());
            
            startSequ.FlowOut("1").ConnectToFlowDestination(setStartIndexVar.FlowIn(Variable_SetNode.IdFlowIn));
            startSequ.FlowOut("2").ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn)); 
            
            completed = branch.FlowOut(Flow_BranchNode.IdFlowOutFalse);
            loopBodyOut = branch.FlowOut(Flow_BranchNode.IdFlowOutTrue);
            
            
            startIndex = setStartIndexVar.ValueIn(Variable_SetNode.IdInputValue);

            var ascendingCondition = unitExporter.CreateNode(new Math_LeNode());
            startIndex = startIndex.Link(ascendingCondition.ValueIn("a"));
            endIndex = ascendingCondition.ValueIn("b");
            
            VariablesHelpers.GetVariable(unitExporter, indexVar, out var indexVarValue);
            currentIndex = indexVarValue;
            
            var addNode = unitExporter.CreateNode(new Math_AddNode());
            addNode.ValueIn("a").ConnectToSource(indexVarValue).SetType(TypeRestriction.LimitToInt);
            step = addNode.ValueIn("b").SetType(TypeRestriction.LimitToInt);
            addNode.FirstValueOut().ExpectedType(ExpectedType.Int);
            
            var setCurrentIndexVar = VariablesHelpers.SetVariable(unitExporter, indexVar);
            setCurrentIndexVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(addNode.FirstValueOut());
            
            var sequence = unitExporter.CreateNode(new Flow_SequenceNode());

            nextStepIn = sequence.FlowIn(Flow_SequenceNode.IdFlowIn);
            sequence.FlowOut("1").ConnectToFlowDestination(setCurrentIndexVar.FlowIn(Variable_SetNode.IdFlowIn));
            sequence.FlowOut("2").ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));     
            
            var ascendingIndexCondition = unitExporter.CreateNode(new Math_LtNode());
            ascendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(ascendingIndexCondition.ValueIn("b"));
            
            var descendingIndexCondition = unitExporter.CreateNode(new Math_GtNode());
            descendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(descendingIndexCondition.ValueIn("b"));
            
            var conditionSelect = unitExporter.CreateNode(new Math_SelectNode());
            conditionSelect.ValueIn("a").ConnectToSource(ascendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("b").ConnectToSource(descendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("condition").ConnectToSource(ascendingCondition.FirstValueOut());
            
            branch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(conditionSelect.FirstValueOut());
        }

        public static void CreateCustomForLoop(UnitExporter unitExporter, 
            out GltfInteractivityUnitExporterNode.ValueInputSocketData startIndex,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData endIndex,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData step,
            out GltfInteractivityUnitExporterNode.FlowInSocketData flowIn,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData currentIndex,
            out GltfInteractivityUnitExporterNode.FlowOutSocketData loopBodyOut,
            out GltfInteractivityUnitExporterNode.FlowOutSocketData completed)
        {
            var indexVar = unitExporter.exportContext.AddVariableWithIdIfNeeded("ForLoopIndex"+System.Guid.NewGuid().ToString(), 0, VariableKind.Scene, typeof(int));

            var whileNode = unitExporter.CreateNode(new Flow_WhileNode());
            var setStartIndexVar = VariablesHelpers.SetVariable(unitExporter, indexVar);
           
            flowIn = setStartIndexVar.FlowIn(Variable_SetNode.IdFlowIn);
            setStartIndexVar.FlowOut(Variable_SetNode.IdFlowOut)
                .ConnectToFlowDestination(whileNode.FlowIn(Flow_WhileNode.IdFlowIn));
            completed = whileNode.FlowOut(Flow_WhileNode.IdCompleted);
            
            startIndex = setStartIndexVar.ValueIn(Variable_SetNode.IdInputValue);

            var ascendingCondition = unitExporter.CreateNode(new Math_LeNode());
            startIndex = startIndex.Link(ascendingCondition.ValueIn("a"));
            endIndex = ascendingCondition.ValueIn("b");
            
            VariablesHelpers.GetVariable(unitExporter, indexVar, out var indexVarValue);
            currentIndex = indexVarValue;
            
            var addNode = unitExporter.CreateNode(new Math_AddNode());
            addNode.ValueIn("a").ConnectToSource(indexVarValue).SetType(TypeRestriction.LimitToInt);
            step = addNode.ValueIn("b").SetType(TypeRestriction.LimitToInt);
            addNode.FirstValueOut().ExpectedType(ExpectedType.Int);
            
            var setCurrentIndexVar = VariablesHelpers.SetVariable(unitExporter, indexVar);
            setCurrentIndexVar.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(addNode.FirstValueOut());
            
            var sequence = unitExporter.CreateNode(new Flow_SequenceNode());
            whileNode.FlowOut(Flow_WhileNode.IdLoopBody).ConnectToFlowDestination(sequence.FlowIn(Flow_SequenceNode.IdFlowIn));

            loopBodyOut = sequence.FlowOut("0");
            sequence.FlowOut("1").ConnectToFlowDestination(setCurrentIndexVar.FlowIn(Variable_SetNode.IdFlowIn));
            
            var ascendingIndexCondition = unitExporter.CreateNode(new Math_LtNode());
            ascendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(ascendingIndexCondition.ValueIn("b"));
            
            var descendingIndexCondition = unitExporter.CreateNode(new Math_GtNode());
            descendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(descendingIndexCondition.ValueIn("b"));
            
            var conditionSelect = unitExporter.CreateNode(new Math_SelectNode());
            conditionSelect.ValueIn("a").ConnectToSource(ascendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("b").ConnectToSource(descendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("condition").ConnectToSource(ascendingCondition.FirstValueOut());
            
            whileNode.ValueIn(Flow_WhileNode.IdCondition).ConnectToSource(conditionSelect.FirstValueOut());
        }

        public static void CreateConditionalWaiting(UnitExporter unitExporter,
            out GltfInteractivityUnitExporterNode.ValueInputSocketData condition, 
            out GltfInteractivityUnitExporterNode.FlowInSocketData flowIn,
            bool waitForTrue,
            out GltfInteractivityUnitExporterNode.FlowOutSocketData flowOutWhenDone)
        {
            var setVarStart = unitExporter.CreateNode(new Variable_SetNode());
            var setVarFinish = unitExporter.CreateNode(new Variable_SetNode());
            var getVar = unitExporter.CreateNode(new Variable_GetNode());
            var varId = unitExporter.exportContext.AddVariableWithIdIfNeeded("waitWhile"+System.Guid.NewGuid(), false, VariableKind.Graph, typeof(bool));
            var tick = unitExporter.CreateNode(new Event_OnTickNode());
            var branch = unitExporter.CreateNode(new Flow_BranchNode());
            var waitingBranch = unitExporter.CreateNode(new Flow_BranchNode());
            
            setVarStart.Configuration[Variable_SetNode.IdConfigVarIndex].Value = varId;
            setVarFinish.Configuration[Variable_SetNode.IdConfigVarIndex].Value = varId;
            setVarFinish.ValueIn(Variable_SetNode.IdInputValue).SetValue(false);
            setVarStart.ValueIn(Variable_SetNode.IdInputValue).SetValue(true);
            
            getVar.Configuration[Variable_SetNode.IdConfigVarIndex].Value = varId;

            flowIn = setVarStart.FlowIn(Variable_SetNode.IdFlowIn); 
            setVarStart.FlowOut(Variable_SetNode.IdFlowOut)
                .ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));
            
            tick.FlowOut(Event_OnTickNode.IdFlowOut).ConnectToFlowDestination(waitingBranch.FlowIn(Flow_BranchNode.IdFlowIn));
            waitingBranch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(getVar.FirstValueOut());
            waitingBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue)
                .ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));

            condition = branch.ValueIn(Flow_BranchNode.IdCondition);

            var conditionFlow = waitForTrue ? Flow_BranchNode.IdFlowOutTrue : Flow_BranchNode.IdFlowOutFalse;
            branch.FlowOut(conditionFlow)
                .ConnectToFlowDestination(setVarFinish.FlowIn(Variable_SetNode.IdFlowIn));

            flowOutWhenDone = setVarFinish.FlowOut(Variable_SetNode.IdFlowOut);
        }
    }
}