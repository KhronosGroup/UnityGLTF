using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace InteractivityASTGenerator.Generators
{
    /// <summary>
    /// Handles generation of AST for statements and expressions
    /// </summary>
    public static class ASTStatementGenerator
    {
        /// <summary>
        /// Generate AST for statements
        /// </summary>
        public static void GenerateStatementsAST(BlockSyntax block, StringBuilder source, string indent, string astNamespace, SemanticModel semanticModel)
        {
            source.AppendLine($"{indent}Statements = new List<{astNamespace}.StatementInfo>");
            source.AppendLine($"{indent}{{");
            
            // Build AST for each statement
            foreach (var statement in block.Statements)
            {
                GenerateStatementNode(statement, source, indent + "    ", astNamespace, semanticModel);
            }
            
            source.AppendLine($"{indent}}},");
        }
        
        /// <summary>
        /// Generate AST for a single statement node using semantic model for type resolution
        /// </summary>
        public static void GenerateStatementNode(StatementSyntax statement, StringBuilder source, string indent, string astNamespace, SemanticModel semanticModel)
        {
            // Handle different statement types
            if (statement is BlockSyntax blockStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.Block,");
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.StatementInfo>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var childStatement in blockStmt.Statements)
                {
                    GenerateStatementNode(childStatement, source, indent + "        ", astNamespace, semanticModel);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is IfStatementSyntax ifStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.If,");
                
                // Extract condition expression with type information
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                GenerateExpressionNode(ifStmt.Condition, source, indent + "        ", astNamespace, semanticModel);
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (then and else clauses)
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.StatementInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Then clause
                source.AppendLine($"{indent}        new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = {astNamespace}.StatementInfo.StatementKind.ThenClause,");
                source.AppendLine($"{indent}            Children = new List<{astNamespace}.StatementInfo>");
                source.AppendLine($"{indent}            {{");
                
                if (ifStmt.Statement is BlockSyntax thenBlock)
                {
                    foreach (var childStatement in thenBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "                ", astNamespace, semanticModel);
                    }
                }
                else
                {
                    GenerateStatementNode(ifStmt.Statement, source, indent + "                ", astNamespace, semanticModel);
                }
                
                source.AppendLine($"{indent}            }}");
                source.AppendLine($"{indent}        }},");
                
                // Process the else clause if it exists
                if (ifStmt.Else != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.StatementInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = {astNamespace}.StatementInfo.StatementKind.ElseClause,");
                    source.AppendLine($"{indent}            Children = new List<{astNamespace}.StatementInfo>");
                    source.AppendLine($"{indent}            {{");
                    
                    if (ifStmt.Else.Statement is BlockSyntax elseBlock)
                    {
                        foreach (var childStatement in elseBlock.Statements)
                        {
                            GenerateStatementNode(childStatement, source, indent + "                ", astNamespace, semanticModel);
                        }
                    }
                    else
                    {
                        GenerateStatementNode(ifStmt.Else.Statement, source, indent + "                ", astNamespace, semanticModel);
                    }
                    
                    source.AppendLine($"{indent}            }}");
                    source.AppendLine($"{indent}        }},");
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ForStatementSyntax forStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.For,");
                
                // Extract initializer, condition, incrementor with type information
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Initializer
                if (forStmt.Declaration != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ExpressionInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Unknown,");
                    
                    // Get the type of the declaration
                    var typeInfo = semanticModel.GetTypeInfo(forStmt.Declaration.Type);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        source.AppendLine($"{indent}            ResultType = typeof({typeName}),");
                    }
                    
                    source.AppendLine($"{indent}        }},");
                }
                
                // Condition
                if (forStmt.Condition != null)
                {
                    GenerateExpressionNode(forStmt.Condition, source, indent + "        ", astNamespace, semanticModel);
                }
                
                // Incrementors
                foreach (var incrementor in forStmt.Incrementors)
                {
                    GenerateExpressionNode(incrementor, source, indent + "        ", astNamespace, semanticModel);
                }
                
                source.AppendLine($"{indent}    }},");
                
                // For body
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.StatementInfo>");
                source.AppendLine($"{indent}    {{");
                
                if (forStmt.Statement is BlockSyntax forBlock)
                {
                    foreach (var childStatement in forBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace, semanticModel);
                    }
                }
                else
                {
                    GenerateStatementNode(forStmt.Statement, source, indent + "        ", astNamespace, semanticModel);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is WhileStatementSyntax whileStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.While,");
                
                // Extract condition with type information
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                GenerateExpressionNode(whileStmt.Condition, source, indent + "        ", astNamespace, semanticModel);
                source.AppendLine($"{indent}    }},");
                
                // While body
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.StatementInfo>");
                source.AppendLine($"{indent}    {{");
                
                if (whileStmt.Statement is BlockSyntax whileBlock)
                {
                    foreach (var childStatement in whileBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace, semanticModel);
                    }
                }
                else
                {
                    GenerateStatementNode(whileStmt.Statement, source, indent + "        ", astNamespace, semanticModel);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ExpressionStatementSyntax exprStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.Expression,");
                
                // Extract the expression with type information
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                GenerateExpressionNode(exprStmt.Expression, source, indent + "        ", astNamespace, semanticModel);
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ReturnStatementSyntax returnStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.Return,");
                
                // Extract the return expression with type information, if present
                if (returnStmt.Expression != null)
                {
                    source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ExpressionInfo>");
                    source.AppendLine($"{indent}    {{");
                    GenerateExpressionNode(returnStmt.Expression, source, indent + "        ", astNamespace, semanticModel);
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}},");
            }
            else if (statement is LocalDeclarationStatementSyntax declStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.Declaration,");
                
                // Extract type information and initializers
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Get the type information
                var typeInfo = semanticModel.GetTypeInfo(declStmt.Declaration.Type);
                string typeName = "object";
                if (typeInfo.Type != null)
                {
                    typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
                
                foreach (var variable in declStmt.Declaration.Variables)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ExpressionInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Identifier,");
                    source.AppendLine($"{indent}            ResultType = typeof({typeName}),");
                    
                    // If there's an initializer, include it with proper type resolution
                    if (variable.Initializer != null)
                    {
                        source.AppendLine($"{indent}            Children = new List<{astNamespace}.ExpressionInfo>");
                        source.AppendLine($"{indent}            {{");
                        GenerateExpressionNode(variable.Initializer.Value, source, indent + "                ", astNamespace, semanticModel);
                        source.AppendLine($"{indent}            }}");
                    }
                    
                    source.AppendLine($"{indent}        }},");
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else
            {
                // Default handling for other statement types
                source.AppendLine($"{indent}new {astNamespace}.StatementInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.StatementInfo.StatementKind.Unknown,");
                source.AppendLine($"{indent}}},");
            }
        }
        
        /// <summary>
        /// Generate AST for an expression node with semantic model for type resolution
        /// </summary>
        public static void GenerateExpressionNode(ExpressionSyntax expression, StringBuilder source, string indent, string astNamespace, SemanticModel semanticModel)
        {
            if (expression == null)
            {
                return;
            }
            
            // Get type information using semantic model
            var typeInfo = semanticModel.GetTypeInfo(expression);
            string typeName = "object";
            if (typeInfo.Type != null)
            {
                typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // Object creation expressions (new Vector3(0,0,0))
            if (expression is ObjectCreationExpressionSyntax objectCreationExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.ObjectCreation,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                
                // Capture argument values for literals
                if (objectCreationExpr.ArgumentList != null && objectCreationExpr.ArgumentList.Arguments.Count > 0)
                {
                    source.AppendLine($"{indent}    Children = new List<{astNamespace}.ExpressionInfo>");
                    source.AppendLine($"{indent}    {{");
                    
                    foreach (var arg in objectCreationExpr.ArgumentList.Arguments)
                    {
                        GenerateExpressionNode(arg.Expression, source, indent + "        ", astNamespace, semanticModel);
                    }
                    
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}},");
            }
            // Method invocations
            else if (expression is InvocationExpressionSyntax invocationExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.MethodInvocation,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                
                // Get method symbol for more detailed information
                var symbolInfo = semanticModel.GetSymbolInfo(invocationExpr);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    string methodName = methodSymbol.Name;
                    string containingTypeName = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    
                    // Generate parameter types array to avoid ambiguity
                    var parameterTypes = new StringBuilder();
                    parameterTypes.Append("new Type[] { ");
                    
                    for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                    {
                        var param = methodSymbol.Parameters[i];
                        string paramTypeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        
                        parameterTypes.Append($"typeof({paramTypeName})");
                        
                        if (i < methodSymbol.Parameters.Length - 1)
                            parameterTypes.Append(", ");
                    }
                    
                    parameterTypes.Append(" }");
                    
                    // Use GetMethod with binding flags and parameter types
                    source.AppendLine($"{indent}    Method = typeof({containingTypeName}).GetMethod(\"{methodName}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, {parameterTypes}, null),");
                }
                
                // Target expression and arguments
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Target expression (e.g., obj in obj.Method())
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    GenerateExpressionNode(memberAccess.Expression, source, indent + "        ", astNamespace, semanticModel);
                }
                
                // Arguments with proper type resolution
                foreach (var arg in invocationExpr.ArgumentList.Arguments)
                {
                    GenerateExpressionNode(arg.Expression, source, indent + "        ", astNamespace, semanticModel);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            // Member access (object.Property)
            else if (expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.MemberAccess,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                
                // Determine if it's a property or field access
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpr.Name);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    string containingTypeName = propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    source.AppendLine($"{indent}    Property = typeof({containingTypeName}).GetProperty(\"{propertySymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),");
                }
                else if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
                {
                    string containingTypeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    source.AppendLine($"{indent}    Field = typeof({containingTypeName}).GetField(\"{fieldSymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),");
                }
                
                // Generate for the object being accessed
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                GenerateExpressionNode(memberAccessExpr.Expression, source, indent + "        ", astNamespace, semanticModel);
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            // Literal values with proper value capture
            else if (expression is LiteralExpressionSyntax literalExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Literal,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                
                // Capture the literal value using the semantic model
                var constantValue = semanticModel.GetConstantValue(expression);
                if (constantValue.HasValue)
                {
                    object value = constantValue.Value;
                    if (value is string stringValue)
                    {
                        source.AppendLine($"{indent}    LiteralValue = \"{stringValue.Replace("\"", "\\\"")}\"");
                    }
                    else if (value == null)
                    {
                        source.AppendLine($"{indent}    LiteralValue = null");
                    }
                    else
                    {
                        source.AppendLine($"{indent}    LiteralValue = {value}");
                    }
                }
                
                source.AppendLine($"{indent}}},");
            }
            // Identifiers (variable names)
            else if (expression is IdentifierNameSyntax identifierExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Identifier,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName})");
                source.AppendLine($"{indent}}},");
            }
            // Binary expressions (a + b, a * b)
            else if (expression is BinaryExpressionSyntax binaryExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Binary,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                source.AppendLine($"{indent}    Operator = \"{binaryExpr.OperatorToken.Text}\",");
                
                // Left and right operands with proper type resolution
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                GenerateExpressionNode(binaryExpr.Left, source, indent + "        ", astNamespace, semanticModel);
                GenerateExpressionNode(binaryExpr.Right, source, indent + "        ", astNamespace, semanticModel);
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            // Assignment expressions (a = b)
            else if (expression is AssignmentExpressionSyntax assignmentExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Assignment,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                source.AppendLine($"{indent}    Operator = \"{assignmentExpr.OperatorToken.Text}\",");
                
                // Left and right expressions
                source.AppendLine($"{indent}    Children = new List<{astNamespace}.ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                GenerateExpressionNode(assignmentExpr.Left, source, indent + "        ", astNamespace, semanticModel);
                GenerateExpressionNode(assignmentExpr.Right, source, indent + "        ", astNamespace, semanticModel);
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            // Parenthesized expressions
            else if (expression is ParenthesizedExpressionSyntax parenthesizedExpr)
            {
                // Process the inner expression directly
                GenerateExpressionNode(parenthesizedExpr.Expression, source, indent, astNamespace, semanticModel);
            }
            // Default handling for other expression types
            else
            {
                source.AppendLine($"{indent}new {astNamespace}.ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = {astNamespace}.ExpressionInfo.ExpressionKind.Unknown,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName})");
                source.AppendLine($"{indent}}},");
            }
        }
    }
}