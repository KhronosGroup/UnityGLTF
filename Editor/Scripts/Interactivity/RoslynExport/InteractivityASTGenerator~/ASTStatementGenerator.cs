using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Collections.Generic;

namespace InteractivityASTGenerator.Generators
{
    /// <summary>
    /// Generates AST for statement nodes
    /// </summary>
    public static class ASTStatementGenerator
    {
        /// <summary>
        /// Generate AST for a statement
        /// </summary>
        public static string GenerateAST(StatementSyntax statement, string indent, SemanticModel semanticModel)
        {
            if (statement == null)
            {
                return $"{indent}null;\n";
            }
            
            var source = new StringBuilder();
            
            source.AppendLine($"{indent}new StatementInfo");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    Kind = StatementInfo.StatementKind.{GetStatementKind(statement)}");
            
            // Handle expression statements
            if (statement is ExpressionStatementSyntax exprStmt)
            {
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Expressions = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                source.Append(ASTExpressionGenerator.GenerateAST(exprStmt.Expression, indent + "        ", semanticModel));
                source.AppendLine();
                source.AppendLine($"{indent}    }}");
            }
            // Handle return statements
            else if (statement is ReturnStatementSyntax returnStmt && returnStmt.Expression != null)
            {
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Expressions = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                source.Append(ASTExpressionGenerator.GenerateAST(returnStmt.Expression, indent + "        ", semanticModel));
                source.AppendLine();
                source.AppendLine($"{indent}    }}");
            }
            // Handle block statements
            else if (statement is BlockSyntax blockStmt)
            {
                if (blockStmt.Statements.Count > 0)
                {
                    source.AppendLine($"{indent},");
                    source.AppendLine($"{indent}    Children = new List<StatementInfo>");
                    source.AppendLine($"{indent}    {{");
                    
                    int stmtIndex = 0;
                    foreach (var childStmt in blockStmt.Statements)
                    {
                        source.Append(GenerateAST(childStmt, indent + "        ", semanticModel));
                        
                        if (stmtIndex < blockStmt.Statements.Count - 1)
                        {
                            source.Append(",");
                        }
                        source.AppendLine();
                        stmtIndex++;
                    }
                    
                    source.AppendLine($"{indent}    }}");
                }
            }
            // Handle if statements
            else if (statement is IfStatementSyntax ifStmt)
            {
                // Handle condition expression
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Expressions = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                source.Append(ASTExpressionGenerator.GenerateAST(ifStmt.Condition, indent + "        ", semanticModel));
                source.AppendLine();
                source.AppendLine($"{indent}    }}");
                
                // Handle then and else statements
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Children = new List<StatementInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Then clause
                source.AppendLine($"{indent}        new StatementInfo");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = StatementInfo.StatementKind.ThenClause");
                
                if (ifStmt.Statement is BlockSyntax thenBlock)
                {
                    if (thenBlock.Statements.Count > 0)
                    {
                        source.AppendLine($"{indent}            ,");
                        source.AppendLine($"{indent}            Children = new List<StatementInfo>");
                        source.AppendLine($"{indent}            {{");
                        
                        int thenStmtIndex = 0;
                        foreach (var thenStmt in thenBlock.Statements)
                        {
                            source.Append(GenerateAST(thenStmt, indent + "                ", semanticModel));
                            
                            if (thenStmtIndex < thenBlock.Statements.Count - 1)
                            {
                                source.Append(",");
                            }
                            source.AppendLine();
                            thenStmtIndex++;
                        }
                        
                        source.AppendLine($"{indent}            }}");
                    }
                }
                else
                {
                    source.AppendLine($"{indent}            ,");
                    source.AppendLine($"{indent}            Children = new List<StatementInfo>");
                    source.AppendLine($"{indent}            {{");
                    source.Append(GenerateAST(ifStmt.Statement, indent + "                ", semanticModel));
                    source.AppendLine();
                    source.AppendLine($"{indent}            }}");
                }
                
                source.AppendLine($"{indent}        }}");
                
                // Else clause if present
                if (ifStmt.Else != null)
                {
                    source.AppendLine($"{indent}        ,");
                    source.AppendLine($"{indent}        new StatementInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = StatementInfo.StatementKind.ElseClause");
                    
                    if (ifStmt.Else.Statement is BlockSyntax elseBlock)
                    {
                        if (elseBlock.Statements.Count > 0)
                        {
                            source.AppendLine($"{indent}            ,");
                            source.AppendLine($"{indent}            Children = new List<StatementInfo>");
                            source.AppendLine($"{indent}            {{");
                            
                            int elseStmtIndex = 0;
                            foreach (var elseStmt in elseBlock.Statements)
                            {
                                source.Append(GenerateAST(elseStmt, indent + "                ", semanticModel));
                                
                                if (elseStmtIndex < elseBlock.Statements.Count - 1)
                                {
                                    source.Append(",");
                                }
                                source.AppendLine();
                                elseStmtIndex++;
                            }
                            
                            source.AppendLine($"{indent}            }}");
                        }
                    }
                    else
                    {
                        source.AppendLine($"{indent}            ,");
                        source.AppendLine($"{indent}            Children = new List<StatementInfo>");
                        source.AppendLine($"{indent}            {{");
                        source.Append(GenerateAST(ifStmt.Else.Statement, indent + "                ", semanticModel));
                        source.AppendLine();
                        source.AppendLine($"{indent}            }}");
                    }
                    
                    source.AppendLine($"{indent}        }}");
                }
                
                source.AppendLine($"{indent}    }}");
            }
            // Handle for statements
            else if (statement is ForStatementSyntax forStmt)
            {
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Expressions = new List<ExpressionInfo>");
                source.AppendLine($"{indent}    {{");
                
                // Process initializer
                if (forStmt.Declaration != null)
                {
                    source.AppendLine($"{indent}        new ExpressionInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = ExpressionInfo.ExpressionKind.ForInitializer");
                    
                    // Get type information for initializer
                    var typeInfo = semanticModel.GetTypeInfo(forStmt.Declaration.Type);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        source.AppendLine($"{indent}            ,");
                        source.AppendLine($"{indent}            ResultType = typeof({typeName})");
                    }
                    
                    // Process variables and their initializers
                    if (forStmt.Declaration.Variables.Count > 0)
                    {
                        source.AppendLine($"{indent}            ,");
                        source.AppendLine($"{indent}            Children = new List<ExpressionInfo>");
                        source.AppendLine($"{indent}            {{");
                        
                        int varIndex = 0;
                        foreach (var variable in forStmt.Declaration.Variables)
                        {
                            if (variable.Initializer != null)
                            {
                                source.AppendLine($"{indent}                new ExpressionInfo");
                                source.AppendLine($"{indent}                {{");
                                source.AppendLine($"{indent}                    Kind = ExpressionInfo.ExpressionKind.Assignment,");
                                source.AppendLine($"{indent}                    Children = new List<ExpressionInfo>");
                                source.AppendLine($"{indent}                    {{");
                                
                                // Left side (identifier)
                                source.AppendLine($"{indent}                        new ExpressionInfo");
                                source.AppendLine($"{indent}                        {{");
                                source.AppendLine($"{indent}                            Kind = ExpressionInfo.ExpressionKind.Identifier,");
                                
                                if (typeInfo.Type != null)
                                {
                                    string typeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    source.AppendLine($"{indent}                            ResultType = typeof({typeName}),");
                                }
                                
                                source.AppendLine($"{indent}                            LiteralValue = \"{variable.Identifier.Text}\"");
                                source.AppendLine($"{indent}                        }},");
                                
                                // Right side (initializer value)
                                source.Append(ASTExpressionGenerator.GenerateAST(variable.Initializer.Value, indent + "                        ", semanticModel));
                                source.AppendLine();
                                
                                source.AppendLine($"{indent}                    }}");
                                source.AppendLine($"{indent}                }}");
                                
                                if (varIndex < forStmt.Declaration.Variables.Count - 1)
                                {
                                    source.Append(",");
                                }
                                source.AppendLine();
                                varIndex++;
                            }
                        }
                        
                        source.AppendLine($"{indent}            }}");
                    }
                    
                    source.AppendLine($"{indent}        }},");
                }
                
                // Process condition
                if (forStmt.Condition != null)
                {
                    source.AppendLine($"{indent}        new ExpressionInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = ExpressionInfo.ExpressionKind.ForCondition,");
                    source.AppendLine($"{indent}            Children = new List<ExpressionInfo>");
                    source.AppendLine($"{indent}            {{");
                    source.Append(ASTExpressionGenerator.GenerateAST(forStmt.Condition, indent + "                ", semanticModel));
                    source.AppendLine();
                    source.AppendLine($"{indent}            }}");
                    source.AppendLine($"{indent}        }},");
                }
                
                // Process incrementors
                int incIdx = 0;
                foreach (var incrementor in forStmt.Incrementors)
                {
                    source.AppendLine($"{indent}        new ExpressionInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = ExpressionInfo.ExpressionKind.ForIncrementor,");
                    source.AppendLine($"{indent}            Children = new List<ExpressionInfo>");
                    source.AppendLine($"{indent}            {{");
                    source.Append(ASTExpressionGenerator.GenerateAST(incrementor, indent + "                ", semanticModel));
                    source.AppendLine();
                    source.AppendLine($"{indent}            }}");
                    source.AppendLine($"{indent}        }}");
                    
                    if (incIdx < forStmt.Incrementors.Count - 1)
                    {
                        source.Append(",");
                    }
                    source.AppendLine();
                    incIdx++;
                }
                
                source.AppendLine($"{indent}    }}");
                
                // Handle for loop body
                source.AppendLine($"{indent},");
                source.AppendLine($"{indent}    Children = new List<StatementInfo>");
                source.AppendLine($"{indent}    {{");
                
                if (forStmt.Statement is BlockSyntax forBlock)
                {
                    int bodyStmtIndex = 0;
                    foreach (var bodyStmt in forBlock.Statements)
                    {
                        source.Append(GenerateAST(bodyStmt, indent + "        ", semanticModel));
                        
                        if (bodyStmtIndex < forBlock.Statements.Count - 1)
                        {
                            source.Append(",");
                        }
                        source.AppendLine();
                        bodyStmtIndex++;
                    }
                }
                else
                {
                    source.Append(GenerateAST(forStmt.Statement, indent + "        ", semanticModel));
                    source.AppendLine();
                }
                
                source.AppendLine($"{indent}    }}");
            }
            
            source.AppendLine($"{indent}}}");
            
            return source.ToString();
        }

        /// <summary>
        /// Get the statement kind string for a statement node
        /// </summary>
        private static string GetStatementKind(StatementSyntax statement)
        {
            if (statement is BlockSyntax) return "Block";
            if (statement is IfStatementSyntax) return "If";
            if (statement is ForStatementSyntax) return "For";
            if (statement is ForEachStatementSyntax) return "ForEach";
            if (statement is WhileStatementSyntax) return "While";
            if (statement is DoStatementSyntax) return "Do";
            if (statement is SwitchStatementSyntax) return "Switch";
            if (statement is ExpressionStatementSyntax) return "Expression";
            if (statement is ReturnStatementSyntax) return "Return";
            if (statement is LocalDeclarationStatementSyntax) return "Declaration";
            if (statement is BreakStatementSyntax) return "Break";
            if (statement is ContinueStatementSyntax) return "Continue";
            if (statement is ThrowStatementSyntax) return "Throw";
            if (statement is TryStatementSyntax) return "Try";
            
            return "Unknown";
        }
    }
}