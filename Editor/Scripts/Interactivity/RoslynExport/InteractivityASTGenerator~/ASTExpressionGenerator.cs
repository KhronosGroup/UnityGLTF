using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace InteractivityASTGenerator.Generators
{
    /// <summary>
    /// Generates AST for expression nodes
    /// </summary>
    public static class ASTExpressionGenerator
    {
        /// <summary>
        /// Generate AST for an expression
        /// </summary>
        public static string GenerateAST(ExpressionSyntax expression, string indent, SemanticModel semanticModel)
        {
            if (expression == null)
            {
                return $"{indent}null;\n";
            }

            var source = new StringBuilder();
            
            // Get type information using semantic model
            var typeInfo = semanticModel.GetTypeInfo(expression);
            string typeName = "object";
            if (typeInfo.Type != null)
            {
                typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            
            // Object creation expressions (e.g. new Vector3(0,0,0))
            if (expression is ObjectCreationExpressionSyntax objectCreationExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.ObjectCreation,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName})");
                
                // Arguments list
                if (objectCreationExpr.ArgumentList != null && objectCreationExpr.ArgumentList.Arguments.Count > 0)
                {
                    source.AppendLine($"{indent},");
                    source.AppendLine($"{indent}    Children = new List<ExpressionInfo>");
                    source.AppendLine($"{indent}    {{");
                    
                    int argIndex = 0;
                    foreach (var arg in objectCreationExpr.ArgumentList.Arguments)
                    {
                        source.Append(GenerateAST(arg.Expression, indent + "        ", semanticModel));
                        
                        if (argIndex < objectCreationExpr.ArgumentList.Arguments.Count - 1)
                        {
                            source.Append(",");
                        }
                        source.AppendLine();
                        argIndex++;
                    }
                    
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}}");
            }
            // Binary expressions (e.g. a + b)
            else if (expression is BinaryExpressionSyntax binaryExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.Binary,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                source.AppendLine($"{indent}    Operator = \"{binaryExpr.OperatorToken.Text}\",");
                source.AppendLine($"{indent}    Children = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Left and right expressions
                source.Append(GenerateAST(binaryExpr.Left, indent + "        ", semanticModel));
                source.Append(",");
                source.AppendLine();
                
                source.Append(GenerateAST(binaryExpr.Right, indent + "        ", semanticModel));
                source.AppendLine();
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}}");
            }
            // Assignment expressions (a = b)
            else if (expression is AssignmentExpressionSyntax assignmentExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.Assignment,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                source.AppendLine($"{indent}    Operator = \"{assignmentExpr.OperatorToken.Text}\",");
                source.AppendLine($"{indent}    Children = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Left and right expressions
                source.Append(GenerateAST(assignmentExpr.Left, indent + "        ", semanticModel));
                source.Append(",");
                source.AppendLine();
                
                source.Append(GenerateAST(assignmentExpr.Right, indent + "        ", semanticModel));
                source.AppendLine();
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}}");
            }
            // Member access (e.g. obj.Property)
            else if (expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.MemberAccess,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName})");
                
                // Determine if it's a property or field
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpr.Name);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    string containingTypeName = propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    source.AppendLine($"{indent},");
                    source.AppendLine($"{indent}    Property = typeof({containingTypeName}).GetProperty(\"{propertySymbol.Name}\")");
                }
                else if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
                {
                    string containingTypeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    source.AppendLine($"{indent},");
                    source.AppendLine($"{indent}    Field = typeof({containingTypeName}).GetField(\"{fieldSymbol.Name}\")");
                }
                
                // The object being accessed
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Children = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                source.Append(GenerateAST(memberAccessExpr.Expression, indent + "        ", semanticModel));
                source.AppendLine();
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}}");
            }
            // Identifiers (variable names)
            else if (expression is IdentifierNameSyntax identifierExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.Identifier,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                source.AppendLine($"{indent}    LiteralValue = \"{identifierExpr.Identifier.Text}\"");
                source.AppendLine($"{indent}}}");
            }
            // Literal values (e.g. "string", 42, true)
            else if (expression is LiteralExpressionSyntax literalExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.Literal,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName})");
                
                // Get the literal value
                var constantValue = semanticModel.GetConstantValue(expression);
                if (constantValue.HasValue)
                {
                    object value = constantValue.Value;
                    source.AppendLine($"{indent},");
                    if (value is string stringValue)
                    {
                        source.AppendLine($"{indent}    LiteralValue = \"{stringValue.Replace("\"", "\\\"")}\"");
                    }
                    else if (value == null)
                    {
                        source.AppendLine($"{indent}    LiteralValue = null");
                    }
                    else if (value is double doubleValue)
                    {
                        source.AppendLine($"{indent}    LiteralValue = {doubleValue.ToString(CultureInfo.InvariantCulture)}");
                    }
                    else if (value is float floatValue)
                    {
                        source.AppendLine($"{indent}    LiteralValue = {floatValue.ToString(CultureInfo.InvariantCulture)}f");
                    }
                    else if (value is decimal decimalValue)
                    {
                        source.AppendLine($"{indent}    LiteralValue = {decimalValue.ToString(CultureInfo.InvariantCulture)}m");
                    }
                    else
                    {
                        // For other numeric types and boolean values
                        source.AppendLine($"{indent}    LiteralValue = {value.ToString().Replace(",", ".")}");
                    }
                }
                
                source.AppendLine($"{indent}}}");
            }
            // Method invocations
            else if (expression is InvocationExpressionSyntax invocationExpr)
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.MethodInvocation,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName})");
                
                // Get method information if possible
                var symbolInfo = semanticModel.GetSymbolInfo(invocationExpr);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    string containingTypeName = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    
                    // Build parameter types array for precise method resolution
                    string parameterTypesArray;
                    
                    if (methodSymbol.Parameters.Length > 0)
                    {
                        var typesBuilder = new StringBuilder("new Type[] { ");
                        
                        for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                        {
                            var param = methodSymbol.Parameters[i];
                            string paramTypeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            
                            typesBuilder.Append($"typeof({paramTypeName})");
                            
                            if (i < methodSymbol.Parameters.Length - 1)
                                typesBuilder.Append(", ");
                        }
                        
                        typesBuilder.Append(" }");
                        parameterTypesArray = typesBuilder.ToString();
                    }
                    else
                    {
                        parameterTypesArray = "new Type[0]";
                    }
                    
                    source.AppendLine($"{indent},");
                    source.AppendLine($"{indent}    Method = typeof({containingTypeName}).GetMethod(\"{methodSymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, {parameterTypesArray}, null)");
                }
                
                // Target expression and arguments
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Children = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Target expression (e.g. obj in obj.Method())
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    source.Append(GenerateAST(memberAccess.Expression, indent + "        ", semanticModel));
                    
                    // Add comma only if there are arguments
                    if (invocationExpr.ArgumentList.Arguments.Count > 0)
                    {
                        source.Append(",");
                    }
                    source.AppendLine();
                }
                
                // Arguments
                int argIdx = 0;
                foreach (var arg in invocationExpr.ArgumentList.Arguments)
                {
                    source.Append(GenerateAST(arg.Expression, indent + "        ", semanticModel));
                    
                    if (argIdx < invocationExpr.ArgumentList.Arguments.Count - 1)
                    {
                        source.Append(",");
                    }
                    source.AppendLine();
                    argIdx++;
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}}");
            }
            // Default case for any other expression type
            else
            {
                source.AppendLine($"{indent}new ExpressionInfo");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = ExpressionInfo.ExpressionKind.Unknown,");
                source.AppendLine($"{indent}    ResultType = typeof({typeName}),");
                source.AppendLine($"{indent}    LiteralValue = \"{expression.ToString().Replace("\"", "\\\"")}\"");
                source.AppendLine($"{indent}}}");
            }
            
            return source.ToString();
        }
    }
}