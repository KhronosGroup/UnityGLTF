using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Globalization;

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
        public static string GenerateAST(ExpressionSyntax expression, string indent, SemanticModel semanticModel, string varName)
        {
            if (expression == null)
            {
                return $"{indent}var {varName} = null;\n";
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
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.ObjectCreation;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                
                // Arguments list
                if (objectCreationExpr.ArgumentList != null && objectCreationExpr.ArgumentList.Arguments.Count > 0)
                {
                    source.AppendLine($"{indent}{varName}.Children = new List<ExpressionInfo>();");
                    
                    int argIndex = 0;
                    foreach (var arg in objectCreationExpr.ArgumentList.Arguments)
                    {
                        string argVarName = $"{varName}_arg{argIndex}";
                        source.Append(GenerateAST(arg.Expression, indent, semanticModel, argVarName));
                        source.AppendLine($"{indent}{varName}.Children.Add({argVarName});");
                        argIndex++;
                    }
                }
            }
            // Binary expressions (e.g. a + b)
            else if (expression is BinaryExpressionSyntax binaryExpr)
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.Binary;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                source.AppendLine($"{indent}{varName}.Operator = \"{binaryExpr.OperatorToken.Text}\";");
                
                // Left and right expressions
                source.AppendLine($"{indent}{varName}.Children = new List<ExpressionInfo>();");
                
                string leftVarName = $"{varName}_left";
                source.Append(GenerateAST(binaryExpr.Left, indent, semanticModel, leftVarName));
                source.AppendLine($"{indent}{varName}.Children.Add({leftVarName});");
                
                string rightVarName = $"{varName}_right";
                source.Append(GenerateAST(binaryExpr.Right, indent, semanticModel, rightVarName));
                source.AppendLine($"{indent}{varName}.Children.Add({rightVarName});");
            }
            // Assignment expressions (a = b)
            else if (expression is AssignmentExpressionSyntax assignmentExpr)
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.Assignment;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                source.AppendLine($"{indent}{varName}.Operator = \"{assignmentExpr.OperatorToken.Text}\";");
                
                // Left and right expressions
                source.AppendLine($"{indent}{varName}.Children = new List<ExpressionInfo>();");
                
                string leftVarName = $"{varName}_left";
                source.Append(GenerateAST(assignmentExpr.Left, indent, semanticModel, leftVarName));
                source.AppendLine($"{indent}{varName}.Children.Add({leftVarName});");
                
                string rightVarName = $"{varName}_right";
                source.Append(GenerateAST(assignmentExpr.Right, indent, semanticModel, rightVarName));
                source.AppendLine($"{indent}{varName}.Children.Add({rightVarName});");
            }
            // Member access (e.g. obj.Property)
            else if (expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.MemberAccess;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                
                // Determine if it's a property or field
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpr.Name);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    string containingTypeName = propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    source.AppendLine($"{indent}{varName}.Property = typeof({containingTypeName}).GetProperty(\"{propertySymbol.Name}\");");
                }
                else if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
                {
                    string containingTypeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    source.AppendLine($"{indent}{varName}.Field = typeof({containingTypeName}).GetField(\"{fieldSymbol.Name}\");");
                }
                
                // The object being accessed
                source.AppendLine($"{indent}{varName}.Children = new List<ExpressionInfo>();");
                
                string objVarName = $"{varName}_obj";
                source.Append(GenerateAST(memberAccessExpr.Expression, indent, semanticModel, objVarName));
                source.AppendLine($"{indent}{varName}.Children.Add({objVarName});");
            }
            // Identifiers (variable names)
            else if (expression is IdentifierNameSyntax identifierExpr)
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.Identifier;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                source.AppendLine($"{indent}{varName}.LiteralValue = \"{identifierExpr.Identifier.Text}\";");
            }
            // Literal values (e.g. "string", 42, true)
            else if (expression is LiteralExpressionSyntax literalExpr)
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.Literal;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                
                // Get the literal value
                var constantValue = semanticModel.GetConstantValue(expression);
                if (constantValue.HasValue)
                {
                    object value = constantValue.Value;
                    if (value is string stringValue)
                    {
                        source.AppendLine($"{indent}{varName}.LiteralValue = \"{stringValue.Replace("\"", "\\\"")}\";");
                    }
                    else if (value == null)
                    {
                        source.AppendLine($"{indent}{varName}.LiteralValue = null;");
                    }
                    else if (value is double doubleValue)
                    {
                        source.AppendLine($"{indent}{varName}.LiteralValue = {doubleValue.ToString(CultureInfo.InvariantCulture)};");
                    }
                    else if (value is float floatValue)
                    {
                        source.AppendLine($"{indent}{varName}.LiteralValue = {floatValue.ToString(CultureInfo.InvariantCulture)}f;");
                    }
                    else if (value is decimal decimalValue)
                    {
                        source.AppendLine($"{indent}{varName}.LiteralValue = {decimalValue.ToString(CultureInfo.InvariantCulture)}m;");
                    }
                    else
                    {
                        // For other numeric types and boolean values
                        source.AppendLine($"{indent}{varName}.LiteralValue = {value.ToString().Replace(",", ".")};");
                    }
                }
            }
            // Method invocations
            else if (expression is InvocationExpressionSyntax invocationExpr)
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.MethodInvocation;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                
                // Target expression and arguments
                source.AppendLine($"{indent}{varName}.Children = new List<ExpressionInfo>();");
                
                // Target expression (e.g. obj in obj.Method())
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    string targetVarName = $"{varName}_target";
                    source.Append(GenerateAST(memberAccess.Expression, indent, semanticModel, targetVarName));
                    source.AppendLine($"{indent}{varName}.Children.Add({targetVarName});");
                }
                
                // Arguments
                int argIdx = 0;
                foreach (var arg in invocationExpr.ArgumentList.Arguments)
                {
                    string argVarName = $"{varName}_arg{argIdx}";
                    source.Append(GenerateAST(arg.Expression, indent, semanticModel, argVarName));
                    source.AppendLine($"{indent}{varName}.Children.Add({argVarName});");
                    argIdx++;
                }
            }
            // Default case for any other expression type
            else
            {
                source.AppendLine($"{indent}var {varName} = new ExpressionInfo();");
                source.AppendLine($"{indent}{varName}.Kind = ExpressionInfo.ExpressionKind.Unknown;");
                source.AppendLine($"{indent}{varName}.ResultType = typeof({typeName});");
                source.AppendLine($"{indent}{varName}.LiteralValue = \"{expression.ToString().Replace("\"", "\\\"")}\";");
            }
            
            return source.ToString();
        }
    }
}