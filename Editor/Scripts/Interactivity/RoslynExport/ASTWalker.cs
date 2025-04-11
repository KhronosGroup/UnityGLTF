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

            // Create a variable set node
            var variableSetNode = context.CreateNode(new Variable_SetNode());
            variableSetNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                { Type = "string" };
            variableSetNode.Configuration["variableName"].Value = variableName;

            // Connect the value to the variable
            variableSetNode.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(initValue);

            // Connect the flow
            inFlow.ConnectToFlowDestination(variableSetNode.FlowIn(Variable_SetNode.IdFlowIn));

            // Create a Variable_Get node that will be used when this variable is referenced
            var getVarNode = context.CreateNode(new Variable_GetNode());
            getVarNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                { Type = "string" };
            getVarNode.Configuration["variableName"].Value = variableName;
            
            // Store the variable for later use
            _variables[variableName] = getVarNode.ValueOut(Variable_GetNode.IdOutputValue);

            return variableSetNode.FlowOut(Variable_SetNode.IdFlowOut);
        }
        else
        {
            // If there's no initializer, just create the variable get node for future references
            var getVarNode = context.CreateNode(new Variable_GetNode());
            getVarNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                { Type = "string" };
            getVarNode.Configuration["variableName"].Value = variableName;
            
            _variables[variableName] = getVarNode.ValueOut(Variable_GetNode.IdOutputValue);
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
                // Create a variable set node
                var variableSetNode = context.CreateNode(new Variable_SetNode());
                variableSetNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                    { Type = "string" };
                variableSetNode.Configuration["variableName"].Value = variableName;

                // Connect the value to the variable
                variableSetNode.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueRef);

                // Connect the flow
                inFlow.ConnectToFlowDestination(variableSetNode.FlowIn(Variable_SetNode.IdFlowIn));

                // Create a Variable_Get node that will be used when this variable is referenced
                var getVarNode = context.CreateNode(new Variable_GetNode());
                getVarNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor
                    { Type = "string" };
                getVarNode.Configuration["variableName"].Value = variableName;
                
                // Store the variable for later use
                _variables[variableName] = getVarNode.ValueOut(Variable_GetNode.IdOutputValue);

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
            var nodeId = context.Context.exporter.ExportNode(gameObject);
            return new LiteralValueRef(nodeId.Id);;
        }
        
        // Check if we have a reference to this variable
        if (!string.IsNullOrEmpty(variableName) && _variables.TryGetValue(variableName, out var variableRef))
        {
            return variableRef;
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
            
            // TODO get type properly
            // TODO handle arrays correctly – currently we're getting "Default constructor not found for type UnityEngine.Vector3[]" because of the Activator.CreateInstance call
            var variableId = context.Context.AddVariableWithIdIfNeeded(variableName, Activator.CreateInstance(expression.ResultType), "float");
            var getVarNode = context.CreateNode(new Variable_GetNode());
            getVarNode.Configuration["variable"].Value = variableId;
            
            // Store it for future references
            var output = getVarNode.ValueOut(Variable_GetNode.IdOutputValue);
            _variables[variableName] = output;
            return output;
        }

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
                var addNode = context.CreateNode(new Math_AddNode());
                
                // Try to determine array length if possible
                int arrayLength = GetArrayLength(targetExpr.LiteralValue?.ToString());
                
                // Set the length as a constant
                addNode.ValueIn("a").SetValue(arrayLength);
                addNode.ValueIn("b").SetValue(0);
                
                return addNode.FirstValueOut();
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

        // Handle static Math methods (like Mathf.Sin)
        if (expression.Method != null && expression.Method.IsStatic && expression.Method.DeclaringType == typeof(Mathf))
        {
            if (expression.Children.Count >= 2)
            {
                var argExpr = expression.Children[1];
                var argRef = ProcessExpression(argExpr);

                if (argRef != null)
                {
                    switch (methodName)
                    {
                        case "Sin":
                            var sinNode = context.CreateNode(new Math_SinNode());
                            sinNode.ValueIn("a").ConnectToSource(argRef);
                            return sinNode.ValueOut("value");
                            
                        case "Cos":
                            var cosNode = context.CreateNode(new Math_CosNode());
                            cosNode.ValueIn("a").ConnectToSource(argRef);
                            return cosNode.ValueOut("value");
                            
                        case "Tan":
                            var tanNode = context.CreateNode(new Math_TanNode());
                            tanNode.ValueIn("a").ConnectToSource(argRef);
                            return tanNode.ValueOut("value");
                    }
                }
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
            return new LiteralValueRef(nodeId.Id);;
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
        if (incrementorExpr.Children[0].Kind == ExpressionInfo.ExpressionKind.Unary &&
            (incrementorExpr.Children[0].Operator == "++" || incrementorExpr.Children[0].Operator == "++"))
        {
            isSimpleIncrement = true;
        }
        // Check for i+=1 pattern
        else if (incrementorExpr.Children[0].Kind == ExpressionInfo.ExpressionKind.Assignment &&
                 incrementorExpr.Children[0].Operator == "+=" &&
                 incrementorExpr.Children[0].Children.Count == 2 &&
                 incrementorExpr.Children[0].Children[1].Kind == ExpressionInfo.ExpressionKind.Literal &&
                 incrementorExpr.Children[0].Children[1].LiteralValue is int intValue && 
                 intValue == 1)
        {
            isSimpleIncrement = true;
        }
        
        if (!isSimpleIncrement)
        {
            Debug.LogWarning("For statement incrementor must be a simple increment (i++ or i+=1) for KHR_interactivity");
            // We'll still continue since we can default to step=1
        }
        
        // Set the step to 1 (this is the only supported value in Flow_ForLoopNode)
        forLoopNode.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
        
        // Connect flow
        inFlow.ConnectToFlowDestination(forLoopNode.FlowIn(Flow_ForLoopNode.IdFlowIn));
        
        // Create a variable get node to access the loop index
        var indexVarNode = context.CreateNode(new Variable_GetNode());
        indexVarNode.Schema.Configuration["variableName"] = new GltfInteractivityNodeSchema.ConfigDescriptor { Type = "string" };
        indexVarNode.Schema.Configuration["variableName"].Type = loopVarName;
        
        /* TODO fix API usage
        // Connect the loop index output to the variable
        forLoopNode.ValueOut(Flow_ForLoopNode.IdIndex).ConnectToFlowDestination(indexVarNode.ValueIn(Variable_GetNode.IdInputVariableName));
        */
        
        // Store a reference to the loop variable for use inside the loop body
        _variables[loopVarName] = forLoopNode.ValueOut(Flow_ForLoopNode.IdIndex);
        
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
            // Try to get the field directly from the current instance
            var field = _classInfo.Type.GetField(arrayName, 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null && field.FieldType.IsArray)
            {
                // Get the array from the instance
                var array = field.GetValue(instance) as Array;
                if (array != null)
                {
                    return array.Length;
                }
            }
            
            // Try to get as a property
            var property = _classInfo.Type.GetProperty(arrayName,
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (property != null && property.PropertyType.IsArray)
            {
                // Get the array from the property
                var array = property.GetValue(instance) as Array;
                if (array != null)
                {
                    return array.Length;
                }
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