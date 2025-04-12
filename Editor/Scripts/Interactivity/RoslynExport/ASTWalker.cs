using System.Reflection;
using System.Threading.Tasks;
using GLTF.Schema;

namespace UnityGLTF.Interactivity.AST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityGLTF.Interactivity.Export;
    using UnityGLTF.Interactivity.Schema;

    
    public class LiteralValueRef : ValueOutRef
    {
        public class TempLiteralNode : GltfInteractivityExportNode
        {
            public object value;
        
            public TempLiteralNode(object value) : base(null)
            {
                Index = int.MaxValue;
                this.value = value;
            }
            
            public override void SetSchema(GltfInteractivityNodeSchema schema, bool applySocketDescriptors,
                bool clearExistingSocketData = true)
            {
            }
        }
        
        public static Dictionary<int, TempLiteralNode> tempNodes = new ();

        public static int StartNodeId = int.MaxValue - tempNodes.Count;

        public static object GetValue(int id)
        {
            if (!tempNodes.TryGetValue(id, out var tempNode))
                return null;
                    
            if (tempNode == null)
            {
                Debug.LogError($"Temp node at index {id} is null");
                return null;
            }

            return tempNode.value;
            
        }
        
        public LiteralValueRef(object value) : base(null, new KeyValuePair<string, GltfInteractivityNode.OutputValueSocketData>("literal", new GltfInteractivityNode.OutputValueSocketData()))
        {
            var tempNode = new TempLiteralNode(value);
            node = tempNode;
            node.Index = int.MaxValue - tempNodes.Count;
            tempNodes.Add(node.Index, tempNode);
        }
        
        public object Value
        {
            get => node as TempLiteralNode != null ? ((TempLiteralNode)node).value : null;
            set => ((TempLiteralNode)node).value = value;
        }
        
        public T ValueAs<T>()
        {
            return (T)(node as TempLiteralNode).value;
        }

        public static void CleanUp()
        {
            LiteralValueRef.tempNodes.Clear();
        }
    }
    
    /// <summary>
    /// A walker that processes ClassReflectionInfo and converts specific methods to GLTF interactivity graphs
    /// </summary>
    public class ClassReflectionASTWalker {
    private readonly ClassReflectionInfo _classInfo;

    // Maps specific method names to their exported flow entry points
    private readonly Dictionary<string, FlowOutRef> _methodEntryPoints = new Dictionary<string, FlowOutRef>();

    /// <summary>
    /// Create a new ClassReflectionASTWalker
    /// </summary>
    /// <param name="classInfo">The ClassReflectionInfo to process</param>
    public ClassReflectionASTWalker(ClassReflectionInfo classInfo)
    {
        _classInfo = classInfo;
    }

    private void ResolveLiterals()
    {
        foreach (var input in context.nodes.SelectMany( n => n.ValueInConnection).Where( inSocket => inSocket.Value.Node >= context.nodes.Count))
        {
            
            var value = LiteralValueRef.GetValue(input.Value.Node.Value);
            if (value == null)
            {
                input.Value.Node = null;
                continue;
            }
            // TODO: Type Conversion (e.g transform and components into int)
            input.Value.Value = value;
            input.Value.Type = GltfTypes.TypeIndex(value.GetType());
            input.Value.Node = null;
        }
        
        LiteralValueRef.CleanUp();
    }

    /// <summary>
    /// Process the ClassReflectionInfo and convert specific methods to GLTF interactivity graphs
    /// </summary>
    /// <returns>The generated interactivity graph</returns>
    public void Process()
    {
        try
        {
            LiteralValueRef.CleanUp();

            // Find and process "Start" method
            ProcessSpecificMethod("Start");

            // Find and process "Update" method
            ProcessSpecificMethod("Update");
            
            ResolveLiterals();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing ClassReflectionInfo: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Process a specific method by name
    /// </summary>
    /// <param name="methodName">The name of the method to process</param>
    private void ProcessSpecificMethod(string methodName)
    {
        var method = _classInfo.Methods.FirstOrDefault(m => m.Name == methodName);
        if (method == null)
        {
            Debug.Log($"No {methodName} method found in class {_classInfo.Type.Name}");
            return;
        }

        // Check if the method body info exists
        if (!_classInfo.MethodBodies.TryGetValue(methodName, out var methodBodyInfo))
        {
            Debug.LogWarning($"Method body info not found for {methodName} in class {_classInfo.Type.Name}");
            return;
        }

        // Create specific event node based on method name
        GltfInteractivityExportNode eventNode;

        if (methodName == "Start")
        {
            eventNode = context.CreateNode(new Event_OnStartNode());
        }
        else if (methodName == "Update")
        {
            eventNode = context.CreateNode(new Event_OnTickNode());
        }
        else
        {
            Debug.LogWarning($"Unsupported method name: {methodName}. Only Start and Update are supported.");
            return;
        }

        // Create an entry flow out reference for this method
        _methodEntryPoints[methodName] = eventNode.FlowOut(methodName == "Start" ? Event_OnStartNode.IdFlowOut : Event_OnTickNode.IdFlowOut);

        // Process the method body
        ProcessMethodBody(methodBodyInfo);
    }

    /// <summary>
    /// Process a method body, converting statements to interactivity nodes
    /// </summary>
    private void ProcessMethodBody(MethodBodyInfo methodBodyInfo)
    {
        var currentFlow = _methodEntryPoints[methodBodyInfo.Method.Name];

        // Process each statement in the method body
        foreach (var statement in methodBodyInfo.Statements)
        {
            // Connect the previous statement to this one via flow
            currentFlow = ProcessStatement(statement, currentFlow);
        }
    }

    /// <summary>
    /// Process a statement and connect it to the previous statement via flow
    /// </summary>
    /// <param name="statement">The statement to process</param>
    /// <param name="inFlow">The incoming flow from the previous statement</param>
    /// <returns>The outgoing flow from this statement</returns>
    private FlowOutRef ProcessStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        switch (statement.Kind)
        {
            case StatementInfo.StatementKind.Expression:
                return ProcessExpressionStatement(statement, inFlow);

            case StatementInfo.StatementKind.Declaration:
                return ProcessDeclarationStatement(statement, inFlow);

            case StatementInfo.StatementKind.Assignment:
                return ProcessAssignmentStatement(statement, inFlow);

            case StatementInfo.StatementKind.If:
                return ProcessIfStatement(statement, inFlow);

            case StatementInfo.StatementKind.Return:
                return ProcessReturnStatement(statement, inFlow);

            case StatementInfo.StatementKind.Block:
                return ProcessBlockStatement(statement, inFlow);
                
            case StatementInfo.StatementKind.For:
                return ProcessForStatement(statement, inFlow);

            default:
                Debug.LogWarning($"Unsupported statement kind: {statement.Kind}");
                return inFlow;
        }
    }

    /// <summary>
    /// Process an expression statement
    /// </summary>
    private FlowOutRef ProcessExpressionStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        if (statement.Expressions.Count == 0)
        {
            return inFlow;
        }

        // An expression statement is typically a method call
        var expression = statement.Expressions[0];
        if (expression.Kind == ExpressionInfo.ExpressionKind.MethodInvocation)
        {
            return ProcessMethodInvocation(expression, inFlow) as FlowOutRef;
        }
        else if (expression.Kind == ExpressionInfo.ExpressionKind.Assignment)
        {
            // Handle assignments within expression statements
            StatementInfo assignmentStatement = new StatementInfo
            {
                Kind = StatementInfo.StatementKind.Assignment,
                Expressions = new List<ExpressionInfo> { expression }
            };
            return ProcessAssignmentStatement(assignmentStatement, inFlow);
        }
        else if (expression.Kind == ExpressionInfo.ExpressionKind.AwaitExpression)
        {
            // For await expressions, process the awaited expression as a statement
            if (expression.Children.Count > 0)
            {
                var childExpr = expression.Children[0];
                
                // If the awaited expression is a Task.Delay call, handle it directly
                if (childExpr.Kind == ExpressionInfo.ExpressionKind.MethodInvocation &&
                    childExpr.Method?.Name == "Delay" &&
                    childExpr.Method.DeclaringType != null &&
                    childExpr.Method.DeclaringType.FullName == "System.Threading.Tasks.Task")
                {
                    return ProcessMethodInvocation(childExpr, inFlow) as FlowOutRef;
                }
                
                // For other types of awaited expressions, just process the child expression
                if (childExpr.Kind == ExpressionInfo.ExpressionKind.MethodInvocation)
                {
                    return ProcessMethodInvocation(childExpr, inFlow) as FlowOutRef;
                }
            }
        }
        else
        {
            Debug.LogWarning($"Unsupported expression type in expression statement: {expression.Kind}");
        }

        return inFlow;
    }

    /// <summary>
    /// Process a declaration statement (e.g. variable declaration)
    /// </summary>
    private FlowOutRef ProcessDeclarationStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        // Get the declaration type expression if it exists
        var declarationTypeExpr = statement.Expressions.FirstOrDefault(e => e.Kind == ExpressionInfo.ExpressionKind.DeclarationType);
        
        // Get the declarator expression which contains the variable name and initialization
        var declaratorExpr = statement.Expressions.FirstOrDefault(e => e.Kind == ExpressionInfo.ExpressionKind.Declarator);
        
        // Get variable name from the declarator expression
        string variableName = declaratorExpr.LiteralValue?.ToString();
        if (string.IsNullOrEmpty(variableName))
        {
            Debug.LogWarning("Failed to determine variable name in declaration statement");
            return inFlow;
        }
        
        // Get variable type from the declaration type or declarator expression
        Type variableType = declarationTypeExpr?.ResultType ?? declaratorExpr.ResultType ?? typeof(object);
        
        // Check if there's an initialization expression
        if (declaratorExpr.Children == null || declaratorExpr.Children.Count == 0)
        {
            // No initialization - just create the variable with default value
            var defaultValue = variableType.IsValueType ? Activator.CreateInstance(variableType) : null;
            var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, defaultValue, variableType?.Name ?? "object");
            
            // Create a variable get node for future references
            var getVarNode = context.CreateNode(new Variable_GetNode());
            getVarNode.Configuration["variable"].Value = variableId;
            
            return inFlow;
        }
        
        // Get the initialization expression
        var initExpr = declaratorExpr.Children[0];
        
        // Process the initialization expression
        ValueOutRef initValue = null;
        
        // Handle await expressions
        if (initExpr.Kind == ExpressionInfo.ExpressionKind.AwaitExpression)
        {
            if (initExpr.Children.Count > 0)
            {
                var childExpr = initExpr.Children[0];
                
                // If the awaited expression is a method invocation, handle it
                if (childExpr.Kind == ExpressionInfo.ExpressionKind.MethodInvocation)
                {
                    var currentFlow = ProcessMethodInvocation(childExpr, inFlow) as FlowOutRef;
                    if (currentFlow != null)
                    {
                        // Create a variable set node with the result
                        var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, 
                            Activator.CreateInstance(initExpr.ResultType ?? variableType ?? typeof(object)), 
                            "float");
                        
                        // Create a variable get node for future references
                        var getVarNode = context.CreateNode(new Variable_GetNode());
                        getVarNode.Configuration["variable"].Value = variableId;
                        
                        return currentFlow;
                    }
                }
            }
        }
        // Handle method invocation
        else if (initExpr.Kind == ExpressionInfo.ExpressionKind.MethodInvocation)
        {
            var currentFlow = ProcessMethodInvocation(initExpr, inFlow) as FlowOutRef;
            if (currentFlow != null)
            {
                // Create a variable set node with the result from the method
                var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, 
                    Activator.CreateInstance(initExpr.ResultType ?? variableType ?? typeof(object)), 
                    initExpr.ResultType?.Name ?? variableType?.Name ?? "object");
                
                // Create a variable get node for future references
                var getVarNode = context.CreateNode(new Variable_GetNode());
                getVarNode.Configuration["variable"].Value = variableId;
                
                return currentFlow;
            }
        }
        // Handle normal expressions by processing them
        else
        {
            initValue = ProcessExpression(initExpr);
        }
        
        // Handle normal expressions with a direct value
        if (initValue != null)
        {
            // Create a variable set node
            var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, 
                Activator.CreateInstance(initExpr.ResultType ?? variableType ?? typeof(object)), 
                initExpr.ResultType?.Name ?? variableType?.Name ?? "object");
            var variableSetNode = context.CreateNode(new Variable_SetNode());
            variableSetNode.Configuration["variable"].Value = variableId;
            
            // Connect the value to the variable
            variableSetNode.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(initValue);
            
            // Connect the flow
            inFlow.ConnectToFlowDestination(variableSetNode.FlowIn(Variable_SetNode.IdFlowIn));
            
            // Create a variable get node for future references
            var getVarNode = context.CreateNode(new Variable_GetNode());
            getVarNode.Configuration["variable"].Value = variableId;
            
            return variableSetNode.FlowOut(Variable_SetNode.IdFlowOut);
        }
        
        return inFlow;
    }
    
    /// <summary>
    /// Helper method to extract variable name from an identifier expression
    /// </summary>
    private string ExtractVariableName(ExpressionInfo identifierExpr)
    {
        string variableName = identifierExpr.LiteralValue?.ToString();
        if (!string.IsNullOrEmpty(variableName))
        {
            return variableName;
        }
        
        // If literal value is null, try to get the variable name from another way
        variableName = identifierExpr.ToString();
        // Remove any "Expression: Identifier (Type: ..." part
        int parenIndex = variableName.IndexOf('(');
        if (parenIndex > 0)
        {
            variableName = variableName.Substring(0, parenIndex).Trim();
            variableName = variableName.Replace("Expression: Identifier", "").Trim();
        }
        
        return variableName;
    }

    /// <summary>
    /// Process an assignment statement
    /// </summary>
    private FlowOutRef ProcessAssignmentStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        if (statement.Expressions.Count < 1)
        {
            return inFlow;
        }

        var assignExpr = statement.Expressions[0];
        if (assignExpr.Kind != ExpressionInfo.ExpressionKind.Assignment || assignExpr.Children.Count < 2)
        {
            return inFlow;
        }

        // Left side of assignment (target)
        var leftExpr = assignExpr.Children[0];

        // Right side of assignment (value)
        var rightExpr = assignExpr.Children[1];
        var valueRef = ProcessExpression(rightExpr);

        if (leftExpr.Kind == ExpressionInfo.ExpressionKind.Identifier)
        {
            // Simple variable assignment
            string variableName = leftExpr.LiteralValue?.ToString();
            if (!string.IsNullOrEmpty(variableName))
            {
                // Create a variable set node
                var variableSetNode = context.CreateNode(new Variable_SetNode());
                var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, Activator.CreateInstance(typeof(object)), "float");
                variableSetNode.Configuration["variable"].Value = variableId;

                // Connect the value to the variable
                variableSetNode.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueRef);

                // Connect the flow
                inFlow.ConnectToFlowDestination(variableSetNode.FlowIn(Variable_SetNode.IdFlowIn));

                // Create a Variable_Get node that will be used when this variable is referenced
                var getVarNode = context.CreateNode(new Variable_GetNode());
                var variableGetId = context.Context.AddVariableWithIdIfNeeded(variableName, Activator.CreateInstance(typeof(object)), "float");
                getVarNode.Configuration["variable"].Value = variableGetId;

                return variableSetNode.FlowOut(Variable_SetNode.IdFlowOut);
            }
        }
        else if (leftExpr.Kind == ExpressionInfo.ExpressionKind.MemberAccess)
        {
            // Processing member access (e.g. transform.position)
            return ProcessMemberAssignment(leftExpr, valueRef, inFlow);
        }

        return inFlow;
    }

    /// <summary>
    /// Process a member assignment (e.g. transform.position = value)
    /// </summary>
    private FlowOutRef ProcessMemberAssignment(ExpressionInfo memberExpr, ValueOutRef valueRef, FlowOutRef inFlow)
    {
        // Check if valueRef is null before proceeding
        if (valueRef == null)
        {
            Debug.LogWarning("Skipping member assignment due to null value reference");
            return inFlow;
        }

        // Check if this is a transform property set
        if (memberExpr.Children.Count > 0 &&
            memberExpr.Children[0].ResultType?.Name == "Transform")
        {
            string propertyName = memberExpr.Property?.Name;

            if (propertyName == "position" || propertyName == "localPosition")
            {
                bool isWorldSpace = propertyName == "position";

                // Get the target object reference
                var targetExpr = ProcessExpression(memberExpr.Children[0]);
                
                // Check if targetExpr is null
                if (targetExpr == null)
                {
                    Debug.LogWarning($"Skipping {propertyName} assignment due to null target expression");
                    return inFlow;
                }

                if (isWorldSpace)
                {
                    ValueInRef target;
                    FlowInRef flowIn;
                    FlowOutRef flowOut;
                    ValueInRef value;

                    // Set position using the position helper
                    TransformHelpers.SetWorldPosition(context, out target, out value, out flowIn, out flowOut);
                    value.ConnectToSource(valueRef);

                    // Check if target is null
                    if (target == null)
                    {
                        Debug.LogWarning("Skipping world position assignment due to null target output from helper");
                        return inFlow;
                    }

                    // Connect the object reference to the target
                    target.ConnectToSource(targetExpr);

                    // Connect the flow
                    inFlow.ConnectToFlowDestination(flowIn);

                    return flowOut;
                }
                else
                {
                    ValueInRef target;
                    FlowInRef flowIn;
                    FlowOutRef flowOut;
                    ValueInRef value;

                    // Set position using the position helper
                    TransformHelpers.SetLocalPosition(context, out target, out value, out flowIn, out flowOut);
                    value.ConnectToSource(valueRef);
                    
                    // Check if target is null
                    if (target == null)
                    {
                        Debug.LogWarning("Skipping local position assignment due to null target output from helper");
                        return inFlow;
                    }

                    // Connect the object reference to the target
                    target.ConnectToSource(targetExpr);

                    // Connect the flow
                    inFlow.ConnectToFlowDestination(flowIn);

                    return flowOut;
                }
            }
            else if (propertyName == "rotation" || propertyName == "localRotation")
            {
                // Similar to position, but for rotation
                // This would use TransformHelpers.SetLocalRotation or equivalent
                // (Implementation omitted for brevity)
            }
            else if (propertyName == "localScale")
            {
                // Implementation for scale
                // (Implementation omitted for brevity)
            }
        }

        return inFlow;
    }

    /// <summary>
    /// Process an if statement
    /// </summary>
    private FlowOutRef ProcessIfStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        // Find the condition expression
        var conditionStatement =
            statement.Children.FirstOrDefault(c => c.Kind == StatementInfo.StatementKind.Condition);
        if (conditionStatement == null || conditionStatement.Expressions.Count == 0)
        {
            return inFlow;
        }

        var conditionExpr = conditionStatement.Expressions[0];
        var conditionValue = ProcessExpression(conditionExpr);

        // Create branch node
        var branchNode = context.CreateNode(new Flow_BranchNode());
        branchNode.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(conditionValue);
        inFlow.ConnectToFlowDestination(branchNode.FlowIn(Flow_BranchNode.IdFlowIn));

        // Process the then clause
        var thenStatement = statement.Children.FirstOrDefault(c => c.Kind == StatementInfo.StatementKind.ThenClause);
        var thenFlow = branchNode.FlowOut(Flow_BranchNode.IdFlowOutTrue);

        if (thenStatement != null)
        {
            foreach (var childStatement in thenStatement.Children)
            {
                thenFlow = ProcessStatement(childStatement, thenFlow);
            }
        }

        // Process the else clause
        var elseStatement = statement.Children.FirstOrDefault(c => c.Kind == StatementInfo.StatementKind.ElseClause);
        var elseFlow = branchNode.FlowOut(Flow_BranchNode.IdFlowOutFalse);

        if (elseStatement != null)
        {
            foreach (var childStatement in elseStatement.Children)
            {
                elseFlow = ProcessStatement(childStatement, elseFlow);
            }
        }

        // In KHR_interactivity, we don't need to join the branches - execution continues from both branches separately
        // Return a null FlowOutRef to indicate that there's no single flow continuation
        // The caller will need to handle this appropriately
        return null;
    }

    /// <summary>
    /// Process a return statement
    /// </summary>
    private FlowOutRef ProcessReturnStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        // For simplicity, we just return the inFlow since returns in Start/Update don't need special handling
        return inFlow;
    }

    /// <summary>
    /// Process a block statement
    /// </summary>
    private FlowOutRef ProcessBlockStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        var currentFlow = inFlow;

        // Process each statement in the block
        foreach (var childStatement in statement.Children)
        {
            currentFlow = ProcessStatement(childStatement, currentFlow);
        }

        return currentFlow;
    }

    /// <summary>
    /// Process a method invocation - both as a statement and as an expression
    /// </summary>
    /// <param name="expression">The method invocation expression</param>
    /// <param name="inFlow">The incoming flow reference (optional, null if called as an expression)</param>
    /// <returns>A flow reference if processing as a statement, or a value reference if called as an expression</returns>
    private object ProcessMethodInvocation(ExpressionInfo expression, FlowOutRef inFlow = null)
    {
        string methodName = expression.Method?.Name;
        bool isStatement = inFlow != null; // If inFlow is provided, we're processing as a statement
        ValueOutRef returnValue = null; // For expression mode

        if (string.IsNullOrEmpty(methodName))
        {
            return isStatement ? inFlow : null; // Return unchanged flow for statements, null value for expressions
        }

        // Handle Task.Delay
        if (methodName == "Delay" && 
            expression.Method.DeclaringType != null && 
            expression.Method.DeclaringType.FullName == "System.Threading.Tasks.Task")
        {   
            // Get the delay duration parameter
            if (expression.Children.Count < 2)
            {
                Debug.LogWarning("Task.Delay call missing duration parameter");
                return inFlow;
            }

            var durationExpr = expression.Children[1];
            var durationRef = ProcessExpression(durationExpr);

            if (durationRef == null)
            {
                Debug.LogWarning("Failed to process Task.Delay duration parameter");
                return inFlow;
            }

            // Create a setDelay node
            var setDelayNode = context.CreateNode(new Flow_SetDelayNode());
            
            // Create a multiply node to convert milliseconds to seconds (multiply by 0.001)
            var multiplyNode = context.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").ConnectToSource(durationRef);
            multiplyNode.ValueIn("b").SetValue(0.001f);
            
            // Connect the converted duration parameter
            setDelayNode.ValueIn(Flow_SetDelayNode.IdDuration).ConnectToSource(multiplyNode.FirstValueOut());
            
            // Connect flow
            inFlow.ConnectToFlowDestination(setDelayNode.FlowIn(Flow_SetDelayNode.IdFlowIn));
            
            // Return the "done" output - this is where execution will continue after the delay
            return setDelayNode.FlowOut(Flow_SetDelayNode.IdFlowDone);
        }

        // Handle GameObject.SetActive
        if (methodName == "SetActive" && 
            expression.Children.Count >= 2 &&
            expression.Children[0].ResultType != null &&
            expression.Children[0].ResultType.Name == "GameObject")
        {
            // Get the target GameObject
            var targetExpr = expression.Children[0];
            var targetRef = ProcessExpression(targetExpr);
            
            // Get the active state parameter
            var activeExpr = expression.Children[1];
            var activeRef = ProcessExpression(activeExpr);

            if (targetRef == null || activeRef == null)
            {
                Debug.LogWarning("Failed to process GameObject.SetActive parameters");
                return inFlow;
            }

            // Create visible and selectable nodes using Pointer_SetNode as in GameObject_SetActiveUnitExport
            var visibleNode = context.CreateNode(new Pointer_SetNode());
            var selectableNode = context.CreateNode(new Pointer_SetNode());

            // Add extensions for visible and selectable
            if (targetRef is LiteralValueRef literalValueRef && literalValueRef.Value is int)
            {
                context.Context.AddVisibilityExtensionToNode(literalValueRef.ValueAs<int>());
                context.Context.AddSelectabilityExtensionToNode(literalValueRef.ValueAs<int>());
            }
            else
            {
                context.Context.AddVisibilityExtensionToAllNodes();
                context.Context.AddSelectabilityExtensionToAllNode();
            }
            
            // Connect flow for visible node
            inFlow.ConnectToFlowDestination(visibleNode.FlowIn(Pointer_SetNode.IdFlowIn));
            
            // Setup pointer template and target input for visible
            PointersHelper.SetupPointerTemplateAndTargetInput(visibleNode, PointersHelper.IdPointerNodeIndex, PointersHelper.IddPointerVisibility, GltfTypes.Bool);
            visibleNode.ValueIn(PointersHelper.IdPointerNodeIndex).ConnectToSource(targetRef);
            
            // Connect active state to visible node
            visibleNode.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(activeRef);
            
            // Connect flow between visible and selectable nodes
            visibleNode.FlowOut(Pointer_SetNode.IdFlowOut)
                .ConnectToFlowDestination(selectableNode.FlowIn(Pointer_SetNode.IdFlowIn));
                
            // Setup pointer template and target input for selectable
            PointersHelper.SetupPointerTemplateAndTargetInput(selectableNode, PointersHelper.IdPointerNodeIndex, PointersHelper.IdPointerSelectability, GltfTypes.Bool);
            selectableNode.ValueIn(PointersHelper.IdPointerNodeIndex).ConnectToSource(targetRef);
            
            // Connect active state to selectable node
            selectableNode.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(activeRef);
            
            // Return the flow out from selectable node
            return selectableNode.FlowOut(Pointer_SetNode.IdFlowOut);
        }
        
        // Handle Debug.Log
        if (methodName == "Log" &&
            expression.Children.Count >= 2 &&
            expression.Children[0].ResultType != null &&
            expression.Children[0].ResultType.Name == "Debug")
        {
            // Get the message parameter
            var messageExpr = expression.Children[1];
            var messageRef = ProcessExpression(messageExpr);

            if (messageRef == null)
            {
                Debug.LogWarning("Failed to process Debug.Log parameters");
                return inFlow;
            }
            
            // Convert the logging format to a pointer template string, 
            // all the parameters become inputs.
            var messageTemplate = "{0}";

            // Create a log node
            var logNode = context.AddLog(GltfInteractivityExportNodes.LogLevel.Info, messageTemplate);
            logNode.ValueIn("0").ConnectToSource(messageRef);
            
            // Connect flow
            inFlow.ConnectToFlowDestination(logNode.FlowIn(Debug_LogNode.IdFlowIn));
            
            // Return the flow out from log node
            return logNode.FlowOut(Debug_LogNode.IdFlowOut);
        }

        // Check if this is a transform-related method
        if (expression.Children.Count > 0 &&
            expression.Children[0].ResultType?.Name == "Transform")
        {
            var targetExpr = ProcessExpression(expression.Children[0]);

            if (isStatement) {
                switch (methodName)
                {
                    case "Translate":
                        return ProcessTranslateMethod(expression, targetExpr, inFlow);

                    case "Rotate":
                        return ProcessRotateMethod(expression, targetExpr, inFlow);

                    case "LookAt":
                        return ProcessLookAtMethod(expression, targetExpr, inFlow);
                }
            }
            // For transform methods that return values, handle here in the future
        }
        
        // Handle Vector3 methods when used as an expression
        if (expression.Children.Count > 0) {
            var targetExpr = expression.Children[0];
            var targetRef = ProcessExpression(targetExpr);
            
            if (targetRef != null) {
                if (targetExpr.ResultType == typeof(Vector3)) {
                    switch (methodName) {
                        case "Normalize":
                            var normalizeNode = context.CreateNode(new Math_NormalizeNode());
                            normalizeNode.ValueIn("a").ConnectToSource(targetRef);
                            returnValue = normalizeNode.ValueOut("value");
                            break;
                            
                        case "Dot":
                            if (expression.Children.Count >= 2) {
                                var argExpr = expression.Children[1];
                                var argRef = ProcessExpression(argExpr);
                                
                                if (argRef != null) {
                                    var dotNode = context.CreateNode(new Math_DotNode());
                                    dotNode.ValueIn("a").ConnectToSource(targetRef);
                                    dotNode.ValueIn("b").ConnectToSource(argRef);
                                    returnValue = dotNode.ValueOut("value");
                                }
                            }
                            break;
                    }
                }
            }
        }
        
        // Handle static Math methods when used as an expression
        if (expression.Method != null && expression.Method.IsStatic && expression.Method.DeclaringType == typeof(Mathf)) {
            if (expression.Children.Count >= 2) {
                var argExpr = expression.Children[1];
                var argRef = ProcessExpression(argExpr);
                
                if (argRef != null) {
                    switch (methodName) {
                        case "Sin":
                            var sinNode = context.CreateNode(new Math_SinNode());
                            sinNode.ValueIn("a").ConnectToSource(argRef);
                            returnValue = sinNode.ValueOut("value");
                            break;
                            
                        case "Cos":
                            var cosNode = context.CreateNode(new Math_CosNode());
                            cosNode.ValueIn("a").ConnectToSource(argRef);
                            returnValue = cosNode.ValueOut("value");
                            break;
                            
                        case "Tan":
                            var tanNode = context.CreateNode(new Math_TanNode());
                            tanNode.ValueIn("a").ConnectToSource(argRef);
                            returnValue = tanNode.ValueOut("value");
                            break;
                    }
                }
            }
        }
        
        // If we're processing as an expression and have a return value, return it
        if (returnValue != null) {
            return returnValue;
        }
        
        // Check if this is a method call to another method in the same class
        if (expression.Method != null)
        {
            // Check if the method exists in the current class
            var classMethod = _classInfo.Methods.FirstOrDefault(m => m.Name == methodName);
            if (classMethod != null && _classInfo.MethodBodies.TryGetValue(methodName, out var methodBodyInfo))
            {
                Debug.Log($"Processing method call to {methodName} within the same class");
                
                // Create a method entry node (similar to a function call)
                var methodEntryNode = context.CreateNode(new Flow_SequenceNode());
                
                // Connect flow from caller to method entry
                inFlow.ConnectToFlowDestination(methodEntryNode.FlowIn(Flow_SequenceNode.IdFlowIn));
                
                // Create a temporary flow out reference to use as the entry point
                var methodEntryFlow = methodEntryNode.FlowOut("0");
                
                // Get the flow out that will be used after the method completes
                var returnFlow = methodEntryNode.FlowOut("1");
                
                // Process the body of the method, starting from our entry flow
                var currentFlow = methodEntryFlow;
                foreach (var statement in methodBodyInfo.Statements)
                {
                    currentFlow = ProcessStatement(statement, currentFlow);
                    
                    // If a statement returns null flow (like an if statement with branches), break the chain
                    if (currentFlow == null)
                    {
                        break;
                    }
                    // If it returns a flow, we attach a WaitAll node so that the return flow only
                    // continues after all branches are done
                    if (currentFlow is FlowOutRef flowOutRef)
                    {
                        var waitAllNode = context.CreateNode(new Flow_WaitAllNode());
                        waitAllNode.Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value = 1;
                        currentFlow.ConnectToFlowDestination(waitAllNode.FlowIn("0"));
                        returnFlow = waitAllNode.FlowOut(Flow_WaitAllNode.IdFlowOutCompleted);
                    }
                }
                
                // Return the flow that continues after the method call
                return returnFlow;
            }
            
            Debug.LogWarning($"Method {methodName} not found in class {_classInfo.Type.Name}");
        }

        // Return appropriate value based on context
        return isStatement ? inFlow : returnValue; 
    }

    /// <summary>
    /// Process a method invocation expression that returns a value
    /// </summary>
    /// <param name="expression">The expression to process</param>
    /// <returns>A reference to the output value of the expression</returns>
    private ValueOutRef ProcessMethodInvocationExpression(ExpressionInfo expression)
    {
        // Use the unified method in expression mode (inFlow = null)
        return ProcessMethodInvocation(expression, null) as ValueOutRef;
    }

    /// <summary>
    /// Process the Translate method of Transform
    /// </summary>
    private FlowOutRef ProcessTranslateMethod(ExpressionInfo expression, ValueOutRef targetRef, FlowOutRef inFlow)
    {
        // Get the translation vector parameter
        if (expression.Children.Count < 2)
        {
            return inFlow;
        }

        var translationExpr = expression.Children[1];
        var translationValue = ProcessExpression(translationExpr);

        // Get current position
        ValueInRef target;
        ValueOutRef currentPos;
        TransformHelpers.GetLocalPosition(context, out target, out currentPos);

        // Connect the object reference to the target
        target.ConnectToSource(targetRef);

        // Add the translation to the current position
        var addNode = context.CreateNode(new Math_AddNode());
        addNode.ValueIn("a").ConnectToSource(currentPos);
        addNode.ValueIn("b").ConnectToSource(translationValue);

        // Set the new position
        FlowInRef flowIn;
        FlowOutRef flowOut;
        TransformHelpers.SetLocalPosition(context, out target, out var valueIn, out flowIn, out flowOut);

        valueIn.ConnectToSource(addNode.FirstValueOut());
        
        target.ConnectToSource(targetRef);

        // Connect the flow
        inFlow.ConnectToFlowDestination(flowIn);

        return flowOut;
    }

    /// <summary>
    /// Process the Rotate method of Transform
    /// </summary>
    private FlowOutRef ProcessRotateMethod(ExpressionInfo expression, ValueOutRef targetRef, FlowOutRef inFlow)
    {
        // This would implement rotation logic similar to Translate
        // (Implementation omitted for brevity)
        return inFlow;
    }

    /// <summary>
    /// Process the LookAt method of Transform
    /// </summary>
    private FlowOutRef ProcessLookAtMethod(ExpressionInfo expression, ValueOutRef targetRef, FlowOutRef inFlow)
    {
        // This would implement look at logic
        // (Implementation omitted for brevity)
        return inFlow;
    }

    /// <summary>
    /// Process an expression and generate the corresponding nodes
    /// </summary>
    /// <param name="expression">The expression to process</param>
    /// <returns>A reference to the output value of the expression</returns>
    private ValueOutRef ProcessExpression(ExpressionInfo expression)
    {
        switch (expression.Kind)
        {
            case ExpressionInfo.ExpressionKind.Literal:
                return ProcessLiteralExpression(expression);

            case ExpressionInfo.ExpressionKind.Identifier:
                return ProcessIdentifierExpression(expression);

            case ExpressionInfo.ExpressionKind.MemberAccess:
                return ProcessMemberAccessExpression(expression);

            case ExpressionInfo.ExpressionKind.MethodInvocation:
                return ProcessMethodInvocationExpression(expression);

            case ExpressionInfo.ExpressionKind.Binary:
                return ProcessBinaryExpression(expression);

            case ExpressionInfo.ExpressionKind.ObjectCreation:
                return ProcessObjectCreationExpression(expression);
                
            case ExpressionInfo.ExpressionKind.ElementAccess:
                return ProcessElementAccessExpression(expression);
                
            case ExpressionInfo.ExpressionKind.Cast:
                // For cast expressions, we simply return the value of the first child
                // Automatic type conversions are handled during graph export
                if (expression.Children.Count > 0)
                {
                    return ProcessExpression(expression.Children[0]);
                }
                Debug.LogWarning("Cast expression has no children");
                return null;

            case ExpressionInfo.ExpressionKind.AwaitExpression:
                // Process the awaited expression
                if (expression.Children.Count > 0)
                {
                    var childExpr = expression.Children[0];
                    
                    // If the awaited expression is a Task.Delay call, we can just process it directly
                    // as the Task.Delay implementation already handles the await behavior
                    if (childExpr.Kind == ExpressionInfo.ExpressionKind.MethodInvocation &&
                        childExpr.Method?.Name == "Delay" &&
                        childExpr.Method.DeclaringType != null &&
                        childExpr.Method.DeclaringType.FullName == "System.Threading.Tasks.Task")
                    {
                        return ProcessExpression(childExpr);
                    }
                    
                    // Otherwise, just process the child expression normally
                    // In the context of a statement, the async/await behavior will be handled 
                    // by the ProcessMethodInvocation method
                    return ProcessExpression(childExpr);
                }
                Debug.LogWarning("Await expression has no children");
                return null;

            default:
                Debug.LogWarning($"Unsupported expression kind: {expression.Kind}. Full expression: {expression}");
                return null;
        }
    }
    
    /// <summary>
    /// Process an element access expression (e.g. array[index])
    /// </summary>
    private ValueOutRef ProcessElementAccessExpression(ExpressionInfo expression)
    {
        if (expression.Children.Count < 2)
        {
            Debug.LogWarning("Element access expression has fewer than 2 children");
            return null;
        }
        
        // First child is the array/collection
        var arrayExpr = expression.Children[0];
        // Second child is the index
        var indexExpr = expression.Children[1];
        
        /*
        var arrayRef = ProcessExpression(arrayExpr);
        */
        
        var indexRef = ProcessExpression(indexExpr);
        
        /*
        if (arrayRef == null || indexRef == null)
        {
            Debug.LogWarning("Could not process array or index reference in element access");
            return null;
        }
        */
        
        // Create an array element node
        /*
        var arrayElementNode = context.CreateNode(new Array_ElementNode());
        arrayElementNode.ValueIn(Array_ElementNode.IdArray).ConnectToSource(arrayRef);
        arrayElementNode.ValueIn(Array_ElementNode.IdIndex).ConnectToSource(indexRef);
        
        return arrayElementNode.ValueOut(Array_ElementNode.IdElement);
        */
        // static access for now
        var instanceArrayValue = GetMemberValue(arrayExpr.LiteralValue?.ToString(), instance);
        if (instanceArrayValue == null) return null;
        var arrayType = instanceArrayValue.GetType();
        var value = arrayType.GetProperty("Item").GetValue(instanceArrayValue, new object[] { indexRef });
        return null;
    }

    /// <summary>
    /// Process an identifier expression (e.g. variable names)
    /// </summary>
    private ValueOutRef ProcessIdentifierExpression(ExpressionInfo expression)
    {
        string variableName = expression.LiteralValue?.ToString();

        var value = GetMemberValue(variableName, instance);
        // TODO this can currently only statically access variables; we might be able to make this dynamic as well by
        // leveraging variable get/set properly and treating the local variables as glTF variables
        if (value is GameObject go)
        {
            var nodeId = context.Context.exporter.ExportNode(gameObject);
            return new LiteralValueRef(nodeId.Id);
        }
        if (value is Transform tr)
        {
            var nodeId = context.Context.exporter.ExportNode(tr.gameObject);
            return new LiteralValueRef(nodeId.Id);
        }
        
        // If we don't have a cached reference but the variable seems to be defined,
        // create a new Variable_Get node to access it
        if (!string.IsNullOrEmpty(variableName))
        {
            if (expression.ResultType.IsArray)
            {
                // TODO handle arrays correctly
                Debug.LogWarning($"Array type not supported for variable: {variableName}");
                return null;
            }

            var resultType = expression.ResultType;
            if (expression.ResultType.IsSubclassOf(typeof(Task)))
            {
                // unwrap the expression and continue with the below
                resultType = expression.ResultType.GetGenericArguments()[0];
            }
            
            // TODO get type properly
            // TODO handle arrays correctly – currently we're getting "Default constructor not found for type UnityEngine.Vector3[]" because of the Activator.CreateInstance call
            var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, Activator.CreateInstance(resultType), "float");
            VariablesHelpers.GetVariable(context, variableId, out var output);
            return output;
        }

        Debug.LogWarning($"Variable '{variableName}' not found in context – neither a method nor a property nor a local variable");
        
        return null;
    }

    /// <summary>
    /// Process a member access expression (e.g. transform.position)
    /// </summary>
    private ValueOutRef ProcessMemberAccessExpression(ExpressionInfo expression)
    {
        // First check for static properties that don't require accessing children
        string propertyName = expression.Property?.Name;
        
        // Check if the expression has information about its target type
        if (expression.Children.Count > 0)
        {
            var targetExpr = expression.Children[0];
            
            // Handle known static properties first
            if (targetExpr.ResultType?.Name == "Time")
            {
                ValueOutRef result;
                // Handle Time properties without recursively processing the Time class
                switch (propertyName)
                {
                    case "deltaTime":
                        TimeHelpers.AddTickNode(context, TimeHelpers.GetTimeValueOption.DeltaTime, out result);
                        return result;
                    
                    case "time":
                    case "fixedTime":
                    case "unscaledTime":
                    case "realtimeSinceStartup":
                        TimeHelpers.AddTickNode(context, TimeHelpers.GetTimeValueOption.TimeSinceStartup, out result);
                        return result;
                }
            }
            
            // Check for Array.Length property
            if (propertyName == "Length" && targetExpr.ResultType != null && targetExpr.ResultType.IsArray)
            {
                // For array length, we'll use a constant value for now based on the actual array instance
               
                // Try to determine array length if possible
                int arrayLength = GetArrayLength(targetExpr.LiteralValue?.ToString());
                return new LiteralValueRef(arrayLength);
            }
            
            // For non-static properties or methods, process the target object
            var targetRef = ProcessExpression(targetExpr);
            
            if (targetRef == null)
            {
                return null;
            }

            // Handle Vector3/Vector4 component access (x, y, z, w)
            if (targetExpr.ResultType == typeof(Vector3) || targetExpr.ResultType == typeof(Vector4))
            {
                switch (propertyName)
                {
                    case "x":
                        var extractXNode = context.CreateNode(new Math_Extract3Node());
                        extractXNode.ValueIn("a").ConnectToSource(targetRef);
                        return extractXNode.ValueOut("value");
                        
                    case "y":
                        var extractYNode = context.CreateNode(new Math_Extract3Node());
                        extractYNode.ValueIn("a").ConnectToSource(targetRef);
                        return extractYNode.ValueOut("value");
                        
                    case "z":
                        var extractZNode = context.CreateNode(new Math_Extract3Node());
                        extractZNode.ValueIn("a").ConnectToSource(targetRef);
                        return extractZNode.ValueOut("value");
                        
                    case "w":
                        // Only valid for Vector4
                        if (targetExpr.ResultType == typeof(Vector4))
                        {
                            var extractWNode = context.CreateNode(new Math_Extract4Node());
                            extractWNode.ValueIn("a").ConnectToSource(targetRef);
                            return extractWNode.ValueOut("value");
                        }
                        Debug.LogWarning("Attempting to access 'w' component on a Vector3");
                        return null;
                }
            }

            // Handle transform properties
            if (targetExpr.ResultType?.Name == "Transform")
            {
                if (propertyName == "position" || propertyName == "localPosition")
                {
                    ValueInRef target;
                    ValueOutRef positionValue;

                    if (propertyName == "position")
                    {
                        TransformHelpers.GetWorldPosition(context, out target, out positionValue);
                    }
                    else
                    {
                        TransformHelpers.GetLocalPosition(context, out target, out positionValue);
                    }

                    // Connect the target object
                    target.ConnectToSource(targetRef);

                    return positionValue;
                }
                else if (propertyName == "rotation" || propertyName == "localRotation")
                {
                    ValueInRef target;
                    ValueOutRef rotationValue;

                    TransformHelpers.GetLocalRotation(context, out target, out rotationValue);

                    // Connect the target object
                    target.ConnectToSource(targetRef);

                    return rotationValue;
                }
                else if (propertyName == "localScale")
                {
                    ValueInRef target;
                    ValueOutRef scaleValue;

                    TransformHelpers.GetLocalScale(context, out target, out scaleValue);

                    // Connect the target object
                    target.ConnectToSource(targetRef);

                    return scaleValue;
                }
            }
            
            // Add other non-static property handlers here
        }
        
        Debug.LogWarning($"Unsupported member access: {propertyName}");
        return null;
    }

    /// <summary>
    /// Process a binary expression (e.g. a + b, a < b)
    /// </summary>
    private ValueOutRef ProcessBinaryExpression(ExpressionInfo expression)
    {
        if (expression.Children.Count < 2)
        {
            return null;
        }

        var leftExpr = expression.Children[0];
        var rightExpr = expression.Children[1];

        var leftRef = ProcessExpression(leftExpr);
        var rightRef = ProcessExpression(rightExpr);

        if (leftRef == null || rightRef == null)
        {
            return null;
        }

        string op = expression.Operator;
        switch (op)
        {
            case "+":
                var addNode = context.CreateNode(new Math_AddNode());
                addNode.ValueIn("a").ConnectToSource(leftRef);
                addNode.ValueIn("b").ConnectToSource(rightRef);
                return addNode.ValueOut("value");

            case "-":
                var subNode = context.CreateNode(new Math_SubNode());
                subNode.ValueIn("a").ConnectToSource(leftRef);
                subNode.ValueIn("b").ConnectToSource(rightRef);
                return subNode.ValueOut("value");

            case "*":
                var mulNode = context.CreateNode(new Math_MulNode());
                mulNode.ValueIn("a").ConnectToSource(leftRef);
                mulNode.ValueIn("b").ConnectToSource(rightRef);
                return mulNode.ValueOut("value");

            case "/":
                var divNode = context.CreateNode(new Math_DivNode());
                divNode.ValueIn("a").ConnectToSource(leftRef);
                divNode.ValueIn("b").ConnectToSource(rightRef);
                return divNode.ValueOut("value");

            case "<":
                var ltNode = context.CreateNode(new Math_LtNode());
                ltNode.ValueIn("a").ConnectToSource(leftRef);
                ltNode.ValueIn("b").ConnectToSource(rightRef);
                return ltNode.ValueOut("value");

            case ">":
                var gtNode = context.CreateNode(new Math_GtNode());
                gtNode.ValueIn("a").ConnectToSource(leftRef);
                gtNode.ValueIn("b").ConnectToSource(rightRef);
                return gtNode.ValueOut("value");

            case "==":
                var eqNode = context.CreateNode(new Math_EqNode());
                eqNode.ValueIn("a").ConnectToSource(leftRef);
                eqNode.ValueIn("b").ConnectToSource(rightRef);
                return eqNode.ValueOut("value");

            default:
                Debug.LogWarning($"Unsupported binary operator: {op}");
                return null;
        }
    }

    /// <summary>
    /// Process an object creation expression (e.g. new Vector3(1, 2, 3))
    /// </summary>
    private ValueOutRef ProcessObjectCreationExpression(ExpressionInfo expression)
    {
        if (expression.ResultType == typeof(Vector3))
        {
            // Create a Vector3 constructor node
            // For simplicity, we assume the arguments are in x, y, z order
            var vec3Node = context.CreateNode(new Math_Combine3Node());

            // Process the constructor arguments
            for (int i = 0; i < Math.Min(expression.Children.Count, 3); i++)
            {
                var argExpr = expression.Children[i];
                var argRef = ProcessExpression(argExpr);

                if (argRef != null)
                {
                    switch (i)
                    {
                        case 0:
                            vec3Node.ValueIn("a").ConnectToSource(argRef);
                            break;
                        case 1:
                            vec3Node.ValueIn("b").ConnectToSource(argRef);
                            break;
                        case 2:
                            vec3Node.ValueIn("c").ConnectToSource(argRef);
                            break;
                    }
                }
            }

            return vec3Node.ValueOut("value");
        }

        return null;
    }


    
    /// <summary>
    /// Process a literal expression (e.g. string, number, bool literals)
    /// </summary>
    private ValueOutRef ProcessLiteralExpression(ExpressionInfo expression)
    {
        var value = expression.LiteralValue;
        if (value == null)
        {
            return null;
        }

        if (value == "transform")
        {
            var nodeId = context.Context.exporter.ExportNode(gameObject);
            return new LiteralValueRef(nodeId.Id);
        }
        else
        {
            context.Context.ConvertValue(value, out var convertedValue, out var type);
            return new LiteralValueRef(convertedValue);
        }
        
        /*

        // Create appropriate constant node based on the type
        if (value is int intValue)
        {
            var constNode = Context.CreateNode(new Math_ConstantNode());
            constNode.Schema.Configuration["value"] = new GltfInteractivityNodeSchema.ConfigDescriptor { Type = "int" };
            constNode.SetConfigValue("value", intValue);
            return constNode.ValueOut("value");
        }
        else if (value is float floatValue || value is double doubleValue)
        {
            var constNode = Context.CreateNode(new Math_ConstantNode());
            constNode.Schema.Configuration["value"] = new GltfInteractivityNodeSchema.ConfigDescriptor { Type = "float" };
            constNode.SetConfigValue("value", value is float ? floatValue : (float)doubleValue);
            return constNode.ValueOut("value");
        }
        else if (value is bool boolValue)
        {
            var constNode = Context.CreateNode(new Math_ConstantNode());
            constNode.Schema.Configuration["value"] = new GltfInteractivityNodeSchema.ConfigDescriptor { Type = "bool" };
            constNode.SetConfigValue("value", boolValue);
            return constNode.ValueOut("value");
        }
        else if (value is string stringValue)
        {
            var constNode = Context.CreateNode(new Math_ConstantNode());
            constNode.Schema.Configuration["value"] = new GltfInteractivityNodeSchema.ConfigDescriptor { Type = "string" };
            constNode.SetConfigValue("value", stringValue);
            return constNode.ValueOut("value");
        }

        */

        return null;
    }

    /// <summary>
    /// Process a for statement
    /// </summary>
    private FlowOutRef ProcessForStatement(StatementInfo statement, FlowOutRef inFlow)
    {
        // Check if this has the expected components of a for loop
        if (statement.Expressions.Count < 1)
        {
            Debug.LogWarning("For statement missing required expressions");
            return inFlow;
        }
        
        // Find initializer, condition, and incrementor expressions
        var initializerExpr = statement.Expressions.FirstOrDefault(e => e.Kind == ExpressionInfo.ExpressionKind.ForInitializer);
        var conditionExpr = statement.Expressions.FirstOrDefault(e => e.Kind == ExpressionInfo.ExpressionKind.ForCondition);
        var incrementorExpr = statement.Expressions.FirstOrDefault(e => e.Kind == ExpressionInfo.ExpressionKind.ForIncrementor);

        // Ensure all required parts are present
        if (initializerExpr == null || conditionExpr == null || incrementorExpr == null)
        {
            Debug.LogWarning("For statement missing required parts (initializer, condition, or incrementor)");
            return inFlow;
        }

        // For KHR_interactivity, the initializer should be a simple variable assignment
        if (initializerExpr.Children.Count == 0 || 
            initializerExpr.Children[0].Kind != ExpressionInfo.ExpressionKind.Assignment || 
            initializerExpr.Children[0].Children.Count < 2)
        {
            Debug.LogWarning("For statement initializer must be a simple variable assignment for KHR_interactivity");
            return inFlow;
        }

        // Extract the loop variable name
        var loopVarExpr = initializerExpr.Children[0].Children[0];
        if (loopVarExpr.Kind != ExpressionInfo.ExpressionKind.Identifier)
        {
            Debug.LogWarning("For statement loop variable must be a simple identifier for KHR_interactivity");
            return inFlow;
        }

        string loopVarName = loopVarExpr.LiteralValue?.ToString();
        if (string.IsNullOrEmpty(loopVarName))
        {
            Debug.LogWarning("For statement loop variable name could not be determined");
            return inFlow;
        }

        // Process the initial value of the loop variable
        var initialValueExpr = initializerExpr.Children[0].Children[1];
        var initialValueRef = ProcessExpression(initialValueExpr);
        if (initialValueRef == null)
        {
            Debug.LogWarning("Failed to process for loop initializer value");
            return inFlow;
        }

        // Process the condition (should result in a boolean value)
        if (conditionExpr.Children.Count == 0)
        {
            Debug.LogWarning("For statement condition is missing");
            return inFlow;
        }

        // Create the for loop node
        var forLoopNode = context.CreateNode(new Flow_ForLoopNode());
        
        // Connect the start index (initial value)
        forLoopNode.ValueIn(Flow_ForLoopNode.IdStartIndex).ConnectToSource(initialValueRef);
        
        // Process and connect the end condition
        // In a for loop, the condition is typically i < endValue
        if (conditionExpr.Children[0].Kind == ExpressionInfo.ExpressionKind.Binary &&
            conditionExpr.Children[0].Operator == "<" && 
            conditionExpr.Children[0].Children.Count == 2)
        {
            var endValueExpr = conditionExpr.Children[0].Children[1];
            var endValueRef = ProcessExpression(endValueExpr);
            
            if (endValueRef == null)
            {
                Debug.LogWarning("Failed to process for loop end condition");
                return inFlow;
            }
            
            forLoopNode.ValueIn(Flow_ForLoopNode.IdEndIndex).ConnectToSource(endValueRef);
        }
        else
        {
            Debug.LogWarning("For statement condition must be a simple comparison (i < endValue) for KHR_interactivity");
            return inFlow;
        }
        
        // Process the incrementor
        // For KHR_interactivity, we only support i++ or i+=1 patterns
        if (incrementorExpr.Children.Count == 0)
        {
            Debug.LogWarning("For statement incrementor is missing");
            return inFlow;
        }
        
        bool isSimpleIncrement = false;
        
        // Check for i++ or ++i pattern
        var child = incrementorExpr.Children[0];
        if ((child.Kind == ExpressionInfo.ExpressionKind.PostfixUnary || child.Kind == ExpressionInfo.ExpressionKind.PrefixUnary) &&
            (child.Operator == "++" || child.Operator == "++"))
        {
            isSimpleIncrement = true;
        }
        // Check for i+=1 pattern
        else if (child.Kind == ExpressionInfo.ExpressionKind.Assignment &&
                 child.Operator == "+=" &&
                 child.Children.Count == 2 &&
                 child.Children[1].Kind == ExpressionInfo.ExpressionKind.Literal &&
                 child.Children[1].LiteralValue is int intValue && 
                 intValue == 1)
        {
            isSimpleIncrement = true;
        }
        
        if (!isSimpleIncrement)
        {
            Debug.LogWarning("For statement incrementor must be a simple increment (i++ or i+=1) for KHR_interactivity");
           
            // TODO: when step != 1, use FlowHelpers.CreateCustomForLoop();
            
            // We'll still continue since we can default to step=1
        }
        
        // Set the step to 1 (this is the only supported value in Flow_ForLoopNode)
        forLoopNode.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
        
        // Connect flow
        inFlow.ConnectToFlowDestination(forLoopNode.FlowIn(Flow_ForLoopNode.IdFlowIn));
        
        // Process the loop body statements
        var bodyFlow = forLoopNode.FlowOut(Flow_ForLoopNode.IdLoopBody);
        
        foreach (var childStatement in statement.Children)
        {
            bodyFlow = ProcessStatement(childStatement, bodyFlow);
            
            // If a statement returns null flow (like an if statement with branches), use the original flow
            if (bodyFlow == null)
            {
                bodyFlow = forLoopNode.FlowOut(Flow_ForLoopNode.IdLoopBody);
                Debug.LogWarning("Flow control inside for loop body may not work as expected");
            }
        }
        
        // Return the completion flow
        return forLoopNode.FlowOut(Flow_ForLoopNode.IdCompleted);
    }

    private MemberInfo GetMemberInfo(string identifier)
    {
        var field = _classInfo.Type.GetField(identifier, 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);

        if (field != null)
            return field;
        
        var property = _classInfo.Type.GetProperty(identifier,
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);

        if (property != null)
            return property;

        return null;
    }

    private object GetMemberValue(string identifier, object instance)
    {
        var memberInfo = GetMemberInfo(identifier);
        
        if (memberInfo is FieldInfo fieldInfo)
        {
            return fieldInfo.GetValue(instance);
        }

        if (memberInfo is PropertyInfo propertyInfo)
        {
            return propertyInfo.GetValue(instance);
        }
        
        // Debug.LogWarning($"Member '{identifier}' not found in type '{_classInfo.Type.Name}'");
        return null;
    }
    
    /// <summary>
    /// Gets the length of an array by name, checking fields, properties, and local variables
    /// </summary>
    /// <param name="arrayName">The name of the array</param>
    /// <returns>The length of the array, or 0 if not found</returns>
    private int GetArrayLength(string arrayName)
    {
        if (string.IsNullOrEmpty(arrayName))
        {
            return 0;
        }
        
        try
        {
            var value = GetMemberValue(arrayName, instance);
            if (value == null)
            {
                return 0;
            }
            
            // Check if the value is an array
            if (value is Array array)
            {
                return array.Length;
            }
            
            
            /* TODO ???
            // It's a local variable, try to get from context if available
            if (_variables.TryGetValue(arrayName, out _))
            {
                if (context.Context != null && 
                    context.Context.TryGetVariableValue(arrayName, out object arrayInstance) && 
                    arrayInstance != null && 
                    arrayInstance.GetType().IsArray)
                {
                    return ((Array)arrayInstance).Length;
                }
            }
            */
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error accessing array length for '{arrayName}': {ex.Message}");
        }
        
        return 0;
    }

    // INodeExporter implementation for creating and managing nodes

    #region INodeExporter Implementation
    
    public GltfInteractivityExportNodes context { get; private set; }
    public GameObject gameObject { get; private set; }
    public object instance { get; private set; }
    
   
    #endregion

    public void OnInteractivityExport(GltfInteractivityExportNodes export, object instance)
    {
        context = export;
        this.instance = instance;
        if (instance is Component component)
            gameObject = component.gameObject;

        try
        {
            // Process the class information into the interactivity graph
            Process();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during interactivity export: {e.Message}\n{e.StackTrace}");
        }
    }
    }
}