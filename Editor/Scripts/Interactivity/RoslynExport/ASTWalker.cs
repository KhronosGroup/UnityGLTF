namespace UnityGLTF.Interactivity.AST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityGLTF.Interactivity.Export;
    using UnityGLTF.Interactivity.Schema;

    /// <summary>
    /// A walker that processes ClassReflectionInfo and converts specific methods to GLTF interactivity graphs
    /// </summary>
    public class ClassReflectionASTWalker

    {
    private readonly ClassReflectionInfo _classInfo;
    private readonly GltfInteractivityGraph _graph;


    // Track variable declarations across method bodies
    private readonly Dictionary<string, ValueOutRef> _variables = new Dictionary<string, ValueOutRef>();

    // Maps specific method names to their exported flow entry points
    private readonly Dictionary<string, FlowOutRef> _methodEntryPoints = new Dictionary<string, FlowOutRef>();

    /// <summary>
    /// Create a new ClassReflectionASTWalker
    /// </summary>
    /// <param name="classInfo">The ClassReflectionInfo to process</param>
    public ClassReflectionASTWalker(ClassReflectionInfo classInfo)
    {
        _classInfo = classInfo;
        _graph = new GltfInteractivityGraph();
    }

    /// <summary>
    /// Process the ClassReflectionInfo and convert specific methods to GLTF interactivity graphs
    /// </summary>
    /// <returns>The generated interactivity graph</returns>
    public void Process()
    {
        try
        {
            // Find and process "Start" method
            ProcessSpecificMethod("Start");

            // Find and process "Update" method
            ProcessSpecificMethod("Update");
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
            return ProcessMethodInvocation(expression, inFlow);
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
        if (statement.Children.Count == 0)
        {
            return inFlow;
        }

        // Find variable declarator child
        var declarator =
            statement.Children.FirstOrDefault(c => c.Kind == StatementInfo.StatementKind.VariableDeclarator);
        if (declarator == null || declarator.Expressions.Count == 0)
        {
            return inFlow;
        }

        // Get variable name from identifier expression
        var identifierExpr =
            declarator.Expressions.FirstOrDefault(e => e.Kind == ExpressionInfo.ExpressionKind.Identifier);
        if (identifierExpr == null)
        {
            return inFlow;
        }

        string variableName = identifierExpr.LiteralValue?.ToString();
        if (string.IsNullOrEmpty(variableName))
        {
            return inFlow;
        }

        // Find initializer expression if any
        var initializer = declarator.Children.FirstOrDefault(c => c.Kind == StatementInfo.StatementKind.Initializer);
        if (initializer != null && initializer.Expressions.Count > 0)
        {
            // Process the initializer expression to get the value
            var initExpr = initializer.Expressions[0];
            var initValue = ProcessExpression(initExpr);

            // Create a variable node
            var variableNode = context.CreateNode(new Variable_SetNode());
            variableNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                { Type = "string" };

            // Connect the value to the variable
            variableNode.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(initValue);

            // Connect the flow
            inFlow.ConnectToFlowDestination(variableNode.FlowIn(Variable_SetNode.IdFlowIn));

            // Store the variable for later use
            var getVarNode = context.CreateNode(new Variable_GetNode());
            getVarNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                { Type = "string" };
            _variables[variableName] = getVarNode.ValueOut(Variable_GetNode.IdOutputValue);

            return variableNode.FlowOut(Variable_SetNode.IdFlowOut);
        }

        return inFlow;
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
                // Create a variable node
                var variableNode = context.CreateNode(new Variable_SetNode());
                variableNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                    { Type = "string" };

                // Connect the value to the variable
                variableNode.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueRef);

                // Connect the flow
                inFlow.ConnectToFlowDestination(variableNode.FlowIn(Variable_SetNode.IdFlowIn));

                // Update the variable reference for later use
                var getVarNode = context.CreateNode(new Variable_GetNode());
                getVarNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                    { Type = "string" };
                _variables[variableName] = getVarNode.ValueOut(Variable_GetNode.IdOutputValue);

                return variableNode.FlowOut(Variable_SetNode.IdFlowOut);
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
                    TransformHelpers.SetWorldPosition(context, out value, out target, out flowIn, out flowOut);
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
                    TransformHelpers.SetLocalPosition(context, out value, out target, out flowIn, out flowOut);
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
    /// Process a method invocation expression
    /// </summary>
    private FlowOutRef ProcessMethodInvocation(ExpressionInfo expression, FlowOutRef inFlow)
    {
        string methodName = expression.Method?.Name;

        if (string.IsNullOrEmpty(methodName))
        {
            return inFlow;
        }

        // Check if this is a transform-related method
        if (expression.Children.Count > 0 &&
            expression.Children[0].ResultType?.Name == "Transform")
        {
            var targetExpr = ProcessExpression(expression.Children[0]);

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

        return inFlow;
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

            default:
                Debug.LogWarning($"Unsupported expression kind: {expression.Kind}");
                return null;
        }
    }

    /// <summary>
    /// Process an identifier expression (e.g. variable names)
    /// </summary>
    private ValueOutRef ProcessIdentifierExpression(ExpressionInfo expression)
    {
        string variableName = expression.LiteralValue?.ToString();

        if (variableName == "transform")
        {
            var addNode = context.CreateNode(new Math_AddNode());
            var nodeId = context.Context.exporter.ExportNode(gameObject);
            addNode.ValueIn("a").SetValue(nodeId.Id);
            addNode.ValueIn("b").SetValue(0);
            return addNode.FirstValueOut();
        }
        
        // Check if we have a reference to this variable
        if (!string.IsNullOrEmpty(variableName) && _variables.TryGetValue(variableName, out var variableRef))
        {
            return variableRef;
        }

        return null;
    }

    /// <summary>
    /// Process a member access expression (e.g. transform.position)
    /// </summary>
    private ValueOutRef ProcessMemberAccessExpression(ExpressionInfo expression)
    {
        if (expression.Children.Count == 0)
        {
            return null;
        }

        // Get the target object (e.g. transform in transform.position)
        var targetExpr = expression.Children[0];
        var targetRef = ProcessExpression(targetExpr);

        if (targetRef == null)
        {
            return null;
        }

        string propertyName = expression.Property?.Name;

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

        return null;
    }

    /// <summary>
    /// Process a method invocation expression that returns a value
    /// </summary>
    private ValueOutRef ProcessMethodInvocationExpression(ExpressionInfo expression)
    {
        // For this implementation, we'll focus on a few common methods
        // In a full implementation, you'd handle many more cases

        string methodName = expression.Method?.Name;

        if (string.IsNullOrEmpty(methodName) || expression.Children.Count == 0)
        {
            return null;
        }

        // Process Vector3 methods
        if (expression.Children[0].ResultType == typeof(Vector3))
        {
            var targetExpr = expression.Children[0];
            var targetRef = ProcessExpression(targetExpr);

            if (targetRef == null)
            {
                return null;
            }

            switch (methodName)
            {
                case "Normalize":
                    var normalizeNode = context.CreateNode(new Math_NormalizeNode());
                    normalizeNode.ValueIn("a").ConnectToSource(targetRef);
                    return normalizeNode.ValueOut("value");

                case "Dot":
                    if (expression.Children.Count >= 2)
                    {
                        var argExpr = expression.Children[1];
                        var argRef = ProcessExpression(argExpr);

                        if (argRef != null)
                        {
                            var dotNode = context.CreateNode(new Math_DotNode());
                            dotNode.ValueIn("a").ConnectToSource(targetRef);
                            dotNode.ValueIn("b").ConnectToSource(argRef);
                            return dotNode.ValueOut("value");
                        }
                    }

                    break;
            }
        }

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
                            vec3Node.ValueIn("x").ConnectToSource(argRef);
                            break;
                        case 1:
                            vec3Node.ValueIn("y").ConnectToSource(argRef);
                            break;
                        case 2:
                            vec3Node.ValueIn("z").ConnectToSource(argRef);
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
        
        var addNode = context.CreateNode(new Math_AddNode());

        if (value == "transform")
        {
            var nodeId = context.Context.exporter.ExportNode(gameObject);
            addNode.ValueIn("a").SetValue(nodeId.Id);
        }
        else if (value is int intValue)
        {
            addNode.ValueIn("a").SetValue(intValue);
        }
        else if (value is float floatValue || value is double doubleValue)
        {
            addNode.ValueIn("a").SetValue(value);
        }
        else if (value is bool boolValue)
        {
            addNode.ValueIn("a").SetValue(boolValue);
        }
        else if (value is string stringValue)
        {
            addNode.ValueIn("a").SetValue(stringValue);
        }
        
        addNode.ValueIn("b").SetValue(0);
        return addNode.FirstValueOut();

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

    // INodeExporter implementation for creating and managing nodes

    #region INodeExporter Implementation
    
    public GltfInteractivityExportNodes context { get; private set; }
    public GameObject gameObject { get; private set; }
    
   
    #endregion

    public void OnInteractivityExport(GltfInteractivityExportNodes export, GameObject gameObject)
    {
        context = export;
        this.gameObject = gameObject;

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