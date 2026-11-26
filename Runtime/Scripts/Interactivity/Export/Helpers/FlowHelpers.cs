using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class FlowHelpers
    {
        public static void CreateCustomForLoopWithFlowStep(INodeExporter exporter,
            out ValueInRef startIndex,
            out ValueInRef endIndex,
            out ValueInRef step,
            out FlowInRef flowIn,
            out FlowInRef nextStepIn,
            out ValueOutRef currentIndex,
            out FlowOutRef loopBodyOut,
            out FlowOutRef completed,
            int breakVariable = -1)
        {
            var indexVar =
                exporter.Context.AddVariableWithIdIfNeeded("ForLoopIndex" + System.Guid.NewGuid().ToString(), 0,
                    GltfTypes.Int);

            var startSequ = exporter.CreateNode<Flow_SequenceNode>();
            flowIn = startSequ.FlowIn(Flow_SequenceNode.IdFlowIn);

            var setStartIndexVar = VariablesHelpers.SetVariable(exporter, indexVar, out startIndex, out _, out _);
            var branch = exporter.CreateNode<Flow_BranchNode>();

            if (breakVariable != -1)
            {
                VariablesHelpers.SetVariableStaticValue(exporter, breakVariable, false, out var resetBreakIn, out _);
                startSequ.FlowOut("0").ConnectToFlowDestination(resetBreakIn);
            }
            startSequ.FlowOut("1").ConnectToFlowDestination(setStartIndexVar.FlowIn(Variable_SetNode.IdFlowIn));
            startSequ.FlowOut("2").ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));

            completed = branch.FlowOut(Flow_BranchNode.IdFlowOutFalse);
            loopBodyOut = branch.FlowOut(Flow_BranchNode.IdFlowOutTrue);


            var ascendingCondition = exporter.CreateNode<Math_LeNode>();
            startIndex = startIndex.Link(ascendingCondition.ValueIn("a"));
            endIndex = ascendingCondition.ValueIn("b");

            VariablesHelpers.GetVariable(exporter, indexVar, out var indexVarValue);
            currentIndex = indexVarValue;

            var addNode = exporter.CreateNode<Math_AddNode>();
            addNode.ValueIn("a").ConnectToSource(indexVarValue).SetType(TypeRestriction.LimitToInt);
            step = addNode.ValueIn("b").SetType(TypeRestriction.LimitToInt);
            addNode.FirstValueOut().ExpectedType(ExpectedType.Int);

            var setCurrentIndexVar = VariablesHelpers.SetVariable(exporter, indexVar, out var setCurrentIndexVarSocket, out _, out _);
            setCurrentIndexVarSocket.ConnectToSource(addNode.FirstValueOut());

            var sequence = exporter.CreateNode<Flow_SequenceNode>();

            nextStepIn = sequence.FlowIn(Flow_SequenceNode.IdFlowIn);
            sequence.FlowOut("1").ConnectToFlowDestination(setCurrentIndexVar.FlowIn(Variable_SetNode.IdFlowIn));
            sequence.FlowOut("2").ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));

            var ascendingIndexCondition = exporter.CreateNode<Math_LtNode>();
            ascendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(ascendingIndexCondition.ValueIn("b"));

            var descendingIndexCondition = exporter.CreateNode<Math_GtNode>();
            descendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(descendingIndexCondition.ValueIn("b"));

            var conditionSelect = exporter.CreateNode<Math_SelectNode>();
            conditionSelect.ValueIn("a").ConnectToSource(ascendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("b").ConnectToSource(descendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("condition").ConnectToSource(ascendingCondition.FirstValueOut());

            branch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(AndNotBroken(exporter, conditionSelect.FirstValueOut(), breakVariable));
        }

        public static void CreateCustomForLoop(INodeExporter exporter,
            out ValueInRef startIndex,
            out ValueInRef endIndex,
            out ValueInRef step,
            out FlowInRef flowIn,
            out ValueOutRef currentIndex,
            out FlowOutRef loopBodyOut,
            out FlowOutRef completed,
            int breakVariable = -1)
        {
            var indexVar = exporter.Context.AddVariableWithIdIfNeeded(
                "ForLoopIndex" + System.Guid.NewGuid().ToString(), 0, GltfTypes.Int);

            var whileNode = exporter.CreateNode<Flow_WhileNode>();
            var setStartIndexVar = VariablesHelpers.SetVariable(exporter, indexVar, out startIndex, out _, out _);

            flowIn = setStartIndexVar.FlowIn(Variable_SetNode.IdFlowIn);
            setStartIndexVar.FlowOut(Variable_SetNode.IdFlowOut)
                .ConnectToFlowDestination(whileNode.FlowIn(Flow_WhileNode.IdFlowIn));
            if (breakVariable != -1)
            {
                VariablesHelpers.SetVariableStaticValue(exporter, breakVariable, false, out var resetBreakIn, out var resetBreakOut);
                resetBreakOut.ConnectToFlowDestination(flowIn);
                flowIn = resetBreakIn;
            }
            completed = whileNode.FlowOut(Flow_WhileNode.IdCompleted);

            var ascendingCondition = exporter.CreateNode<Math_LeNode>();
            startIndex = startIndex.Link(ascendingCondition.ValueIn("a"));
            endIndex = ascendingCondition.ValueIn("b");

            VariablesHelpers.GetVariable(exporter, indexVar, out var indexVarValue);
            currentIndex = indexVarValue;

            var addNode = exporter.CreateNode<Math_AddNode>();
            addNode.ValueIn("a").ConnectToSource(indexVarValue).SetType(TypeRestriction.LimitToInt);
            step = addNode.ValueIn("b").SetType(TypeRestriction.LimitToInt);
            addNode.FirstValueOut().ExpectedType(ExpectedType.Int);

            var setCurrentIndexVar = VariablesHelpers.SetVariable(exporter, indexVar, out var setCurrentIndexVarSocket, out _, out _);
            setCurrentIndexVarSocket.ConnectToSource(addNode.FirstValueOut());

            var sequence = exporter.CreateNode<Flow_SequenceNode>();
            whileNode.FlowOut(Flow_WhileNode.IdLoopBody)
                .ConnectToFlowDestination(sequence.FlowIn(Flow_SequenceNode.IdFlowIn));

            loopBodyOut = sequence.FlowOut("0");
            sequence.FlowOut("1").ConnectToFlowDestination(setCurrentIndexVar.FlowIn(Variable_SetNode.IdFlowIn));

            var ascendingIndexCondition = exporter.CreateNode<Math_LtNode>();
            ascendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(ascendingIndexCondition.ValueIn("b"));

            var descendingIndexCondition = exporter.CreateNode<Math_GtNode>();
            descendingIndexCondition.ValueIn("a").ConnectToSource(indexVarValue);
            endIndex = endIndex.Link(descendingIndexCondition.ValueIn("b"));

            var conditionSelect = exporter.CreateNode<Math_SelectNode>();
            conditionSelect.ValueIn("a").ConnectToSource(ascendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("b").ConnectToSource(descendingIndexCondition.FirstValueOut());
            conditionSelect.ValueIn("condition").ConnectToSource(ascendingCondition.FirstValueOut());

            whileNode.ValueIn(Flow_WhileNode.IdCondition).ConnectToSource(AndNotBroken(exporter, conditionSelect.FirstValueOut(), breakVariable));
        }

        /// <summary>
        /// Adds "AND NOT breakVariable" to a loop condition, so a Break can stop the loop. Returns the condition unchanged without a break variable.
        /// </summary>
        private static ValueOutRef AndNotBroken(INodeExporter exporter, ValueOutRef condition, int breakVariable)
        {
            if (breakVariable == -1)
                return condition;

            VariablesHelpers.GetVariable(exporter, breakVariable, out var broken);
            var not = exporter.CreateNode<Math_NotNode>();
            not.ValueIn(Math_NotNode.IdValueA).ConnectToSource(broken).SetType(TypeRestriction.LimitToBool);
            not.FirstValueOut().ExpectedType(ExpectedType.Bool);

            var and = exporter.CreateNode<Math_AndNode>();
            and.ValueIn(Math_AndNode.IdValueA).ConnectToSource(condition).SetType(TypeRestriction.LimitToBool);
            and.ValueIn(Math_AndNode.IdValueB).ConnectToSource(not.FirstValueOut()).SetType(TypeRestriction.LimitToBool);
            return and.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }

        public static void CreateConditionalWaiting(INodeExporter exporter,
            out ValueInRef condition,
            out FlowInRef flowIn,
            bool waitForTrue,
            out FlowOutRef flowOutWhenDone)
        {
            var getVar = exporter.CreateNode<Variable_GetNode>();
            var varId = exporter.Context.AddVariableWithIdIfNeeded("waitWhile" + System.Guid.NewGuid(), false,
                GltfTypes.Bool);
            var tick = exporter.CreateNode<Event_OnTickNode>();
            var branch = exporter.CreateNode<Flow_BranchNode>();
            var waitingBranch = exporter.CreateNode<Flow_BranchNode>();

            var setVarStart =  VariablesHelpers.SetVariable(exporter, varId, out var setVarStartSocket, out _, out _);
            setVarStartSocket.SetValue(true);
            
            var setVarFinish =  VariablesHelpers.SetVariable(exporter, varId, out var setVarFinishSocket, out _, out _);
            setVarFinishSocket.SetValue(false);

            getVar.Configuration[Variable_GetNode.IdConfigVarIndex].Value = varId;

            flowIn = setVarStart.FlowIn(Variable_SetNode.IdFlowIn);
            setVarStart.FlowOut(Variable_SetNode.IdFlowOut)
                .ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));

            tick.FlowOut(Event_OnTickNode.IdFlowOut)
                .ConnectToFlowDestination(waitingBranch.FlowIn(Flow_BranchNode.IdFlowIn));
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