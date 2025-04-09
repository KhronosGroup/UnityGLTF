using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        public static void GenerateStatementsAST(BlockSyntax block, StringBuilder source, string indent, string astNamespace)
        {
            source.AppendLine($"{indent}Body = new List<{astNamespace}.ASTStatement>");
            source.AppendLine($"{indent}{{");
            
            // Build AST for each statement
            foreach (var statement in block.Statements)
            {
                GenerateStatementNode(statement, source, indent + "    ", astNamespace);
            }
            
            source.AppendLine($"{indent}}},");
        }
        
        /// <summary>
        /// Generate AST for a single statement node
        /// </summary>
        public static void GenerateStatementNode(StatementSyntax statement, StringBuilder source, string indent, string astNamespace)
        {
            // Handle different statement types differently to build a more complete AST
            if (statement is BlockSyntax blockStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTBlockStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"Block\",");
                source.AppendLine($"{indent}    Text = @\"{statement.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    Statements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var childStatement in blockStmt.Statements)
                {
                    GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is IfStatementSyntax ifStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"IfStatement\",");
                source.AppendLine($"{indent}    Text = @\"{ifStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract expressions
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"Condition\",");
                source.AppendLine($"{indent}            Text = @\"{ifStmt.Condition.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}        }}");
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (then clause)
                source.AppendLine($"{indent}    ChildStatements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                // Process the main if body
                source.AppendLine($"{indent}        new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"ThenClause\",");
                source.AppendLine($"{indent}            Text = @\"Then\"");
                source.AppendLine($"{indent}        }},");
                
                if (ifStmt.Statement is BlockSyntax thenBlock)
                {
                    foreach (var childStatement in thenBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                    }
                }
                else
                {
                    GenerateStatementNode(ifStmt.Statement, source, indent + "        ", astNamespace);
                }
                
                // Process the else clause if it exists
                if (ifStmt.Else != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTStatement");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ElseClause\",");
                    source.AppendLine($"{indent}            Text = @\"Else\"");
                    source.AppendLine($"{indent}        }},");
                    
                    if (ifStmt.Else.Statement is BlockSyntax elseBlock)
                    {
                        foreach (var childStatement in elseBlock.Statements)
                        {
                            GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                        }
                    }
                    else
                    {
                        GenerateStatementNode(ifStmt.Else.Statement, source, indent + "        ", astNamespace);
                    }
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ForStatementSyntax forStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ForStatement\",");
                source.AppendLine($"{indent}    Text = @\"{forStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract initializer, condition, incrementor
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Initializer
                if (forStmt.Declaration != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ForInitializer\",");
                    source.AppendLine($"{indent}            Text = @\"{forStmt.Declaration.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }},");
                }
                
                // Condition
                if (forStmt.Condition != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ForCondition\",");
                    source.AppendLine($"{indent}            Text = @\"{forStmt.Condition.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }},");
                }
                
                // Incrementors
                foreach (var incrementor in forStmt.Incrementors)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ForIncrementor\",");
                    source.AppendLine($"{indent}            Text = @\"{incrementor.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }},");
                }
                
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (for body)
                source.AppendLine($"{indent}    ChildStatements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                if (forStmt.Statement is BlockSyntax forBlock)
                {
                    foreach (var childStatement in forBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                    }
                }
                else
                {
                    GenerateStatementNode(forStmt.Statement, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is WhileStatementSyntax whileStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"WhileStatement\",");
                source.AppendLine($"{indent}    Text = @\"{whileStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract condition
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"Condition\",");
                source.AppendLine($"{indent}            Text = @\"{whileStmt.Condition.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}        }}");
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (while body)
                source.AppendLine($"{indent}    ChildStatements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                if (whileStmt.Statement is BlockSyntax whileBlock)
                {
                    foreach (var childStatement in whileBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                    }
                }
                else
                {
                    GenerateStatementNode(whileStmt.Statement, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ExpressionStatementSyntax exprStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ExpressionStatement\",");
                source.AppendLine($"{indent}    Text = @\"{exprStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract the expression
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                GenerateExpressionNode(exprStmt.Expression, source, indent + "        ", astNamespace);
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ReturnStatementSyntax returnStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ReturnStatement\",");
                source.AppendLine($"{indent}    Text = @\"{returnStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Add the return expression if there is one
                if (returnStmt.Expression != null)
                {
                    source.AppendLine($"{indent}    ,");
                    source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                    source.AppendLine($"{indent}    {{");
                    
                    GenerateExpressionNode(returnStmt.Expression, source, indent + "        ", astNamespace);
                    
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}},");
            }
            else if (statement is LocalDeclarationStatementSyntax declStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"LocalDeclaration\",");
                source.AppendLine($"{indent}    Text = @\"{declStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // For each variable declarator, add an expression
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var variable in declStmt.Declaration.Variables)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"VariableDeclarator\",");
                    source.AppendLine($"{indent}            Text = @\"{variable.ToString().Replace("\"", "\"\"")}\"");
                    
                    // If there's an initializer, include it
                    if (variable.Initializer != null)
                    {
                        source.AppendLine($"{indent}            ,");
                        source.AppendLine($"{indent}            ChildExpressions = new List<{astNamespace}.ASTExpression>");
                        source.AppendLine($"{indent}            {{");
                        
                        source.AppendLine($"{indent}                new {astNamespace}.ASTExpression");
                        source.AppendLine($"{indent}                {{");
                        source.AppendLine($"{indent}                    Kind = \"Initializer\",");
                        source.AppendLine($"{indent}                    Text = @\"{variable.Initializer.Value.ToString().Replace("\"", "\"\"")}\"");
                        source.AppendLine($"{indent}                }}");
                        
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
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"{statement.Kind()}\",");
                source.AppendLine($"{indent}    Text = @\"{statement.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}}},");
            }
        }
        
        /// <summary>
        /// Generate AST for an expression node
        /// </summary>
        public static void GenerateExpressionNode(ExpressionSyntax expression, StringBuilder source, string indent, string astNamespace)
        {
            if (expression == null)
            {
                source.AppendLine($"{indent}null,");
                return;
            }

            if (expression is BinaryExpressionSyntax binaryExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"BinaryExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = \"{binaryExpr.OperatorToken.Text}\"");
                source.AppendLine($"{indent}    }},");
                
                source.AppendLine($"{indent}    ChildExpressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Left expression
                GenerateExpressionNode(binaryExpr.Left, source, indent + "        ", astNamespace);
                
                // Right expression
                GenerateExpressionNode(binaryExpr.Right, source, indent + "        ", astNamespace);
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is InvocationExpressionSyntax invocationExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTInvocationExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"InvocationExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = \"MethodCall\"");
                source.AppendLine($"{indent}    }},");
                
                // Generate method info with target
                string methodName = invocationExpr.Expression is MemberAccessExpressionSyntax maExpr 
                    ? maExpr.Name.ToString()
                    : invocationExpr.Expression.ToString();
                
                source.AppendLine($"{indent}    MethodInfo = new {astNamespace}.ASTMethodInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        Name = @\"{methodName.Replace("\"", "\"\"")}\"");
                
                // If it's a member access, we can determine the declaring type
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    string expressionText = memberAccess.Expression.ToString();
                    source.AppendLine($"{indent}        ,");
                    source.AppendLine($"{indent}        DeclaringType = new {astNamespace}.ASTTypeInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            TypeName = @\"{expressionText.Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }}");
                }
                
                source.AppendLine($"{indent}    }},");
                
                // Target expression (e.g., the object the method is called on)
                source.AppendLine($"{indent}    TargetExpression = new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        Kind = \"Target\",");
                source.AppendLine($"{indent}        Text = @\"{invocationExpr.Expression.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}    }},");
                
                // Arguments
                source.AppendLine($"{indent}    Arguments = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var arg in invocationExpr.ArgumentList.Arguments)
                {
                    // For each argument, recursively generate its expression
                    GenerateExpressionNode(arg.Expression, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is AssignmentExpressionSyntax assignmentExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"AssignmentExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = \"{assignmentExpr.OperatorToken.Text}\"");
                source.AppendLine($"{indent}    }},");
                
                source.AppendLine($"{indent}    ChildExpressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Left expression (target of assignment)
                // Instead of simple string representation, use proper recursive parsing
                GenerateExpressionNode(assignmentExpr.Left, source, indent + "        ", astNamespace);
                
                // Right expression (value being assigned) - recursive parsing for complex expressions
                GenerateExpressionNode(assignmentExpr.Right, source, indent + "        ", astNamespace);
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTPropertyAccessExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"PropertyAccessExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = \"{memberAccessExpr.OperatorToken.Text}\"");
                source.AppendLine($"{indent}    }},");
                source.AppendLine($"{indent}    MemberName = @\"{memberAccessExpr.Name.ToString().Replace("\"", "\"\"")}\",");
                
                // The object being accessed
                source.AppendLine($"{indent}    Expression = ");
                GenerateExpressionNode(memberAccessExpr.Expression, source, indent + "    ", astNamespace);
                
                // Add member type info if available
                source.AppendLine($"{indent}    MemberType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = \"Unknown\" // Type resolution would require semantic model");
                source.AppendLine($"{indent}    }}");
                
                source.AppendLine($"{indent}}},");
            }
            else if (expression is ObjectCreationExpressionSyntax objectCreationExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTObjectCreationExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ObjectCreationExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                
                // Detailed type information
                source.AppendLine($"{indent}    CreatedType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = @\"{objectCreationExpr.Type.ToString().Replace("\"", "\"\"")}\"");
                
                // Parse the namespace from the type if possible
                string typeName = objectCreationExpr.Type.ToString();
                int lastDot = typeName.LastIndexOf('.');
                if (lastDot > 0)
                {
                    string ns = typeName.Substring(0, lastDot);
                    source.AppendLine($"{indent}        , Namespace = @\"{ns.Replace("\"", "\"\"")}\"");
                }
                
                // Check if it's a generic type
                if (objectCreationExpr.Type is GenericNameSyntax genericType)
                {
                    source.AppendLine($"{indent}        , IsGeneric = true");
                    source.AppendLine($"{indent}        , TypeArguments = new List<{astNamespace}.ASTTypeInfo>");
                    source.AppendLine($"{indent}        {{");
                    
                    foreach (var typeArg in genericType.TypeArgumentList.Arguments)
                    {
                        source.AppendLine($"{indent}            new {astNamespace}.ASTTypeInfo");
                        source.AppendLine($"{indent}            {{");
                        source.AppendLine($"{indent}                TypeName = @\"{typeArg.ToString().Replace("\"", "\"\"")}\"");
                        source.AppendLine($"{indent}            }},");
                    }
                    
                    source.AppendLine($"{indent}        }}");
                }
                
                source.AppendLine($"{indent}    }},");
                
                // Constructor arguments with proper recursive expression parsing
                source.AppendLine($"{indent}    Arguments = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                if (objectCreationExpr.ArgumentList != null)
                {
                    foreach (var arg in objectCreationExpr.ArgumentList.Arguments)
                    {
                        GenerateExpressionNode(arg.Expression, source, indent + "        ", astNamespace);
                    }
                }
                
                source.AppendLine($"{indent}    }}");
                
                // Handle initializers if present
                if (objectCreationExpr.Initializer != null)
                {
                    source.AppendLine($"{indent}    ,");
                    source.AppendLine($"{indent}    Initializers = new List<{astNamespace}.ASTExpression>");
                    source.AppendLine($"{indent}    {{");
                    
                    foreach (var initializer in objectCreationExpr.Initializer.Expressions)
                    {
                        GenerateExpressionNode(initializer, source, indent + "        ", astNamespace);
                    }
                    
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}},");
            }
            else if (expression is LiteralExpressionSyntax literalExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"Literal\",");
                source.AppendLine($"{indent}    Text = @\"{literalExpr.Token.Text.Replace("\"", "\"\"")}\"");
                
                // Include literal type information
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    ExpressionType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = \"{literalExpr.Token.ValueText}\"");
                source.AppendLine($"{indent}    }}");
                
                source.AppendLine($"{indent}}},");
            }
            else if (expression is IdentifierNameSyntax identifierExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"Identifier\",");
                source.AppendLine($"{indent}    Text = @\"{identifierExpr.Identifier.Text.Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is ParenthesizedExpressionSyntax parenthesizedExpr)
            {
                // For parenthesized expressions, process the expression inside
                GenerateExpressionNode(parenthesizedExpr.Expression, source, indent, astNamespace);
            }
            else
            {
                // Default handling for other expression types
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"UnknownExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}}},");
            }
        }
    }
}