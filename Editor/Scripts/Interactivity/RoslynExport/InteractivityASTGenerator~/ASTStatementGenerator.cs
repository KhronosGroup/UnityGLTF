using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

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
            var source = new StringBuilder();
            
            // Create a uniquely named variable for the statement to avoid scope conflicts
            string statementVarName = $"statement{System.Guid.NewGuid().ToString().Replace("-", "")}";
            
            source.AppendLine($"{indent}var {statementVarName} = new StatementInfo();");
            source.AppendLine($"{indent}{statementVarName}.Kind = StatementInfo.StatementKind.{GetStatementKind(statement)};");
            
            // Handle expression statements specially
            if (statement is ExpressionStatementSyntax exprStmt)
            {
                source.AppendLine($"{indent}{statementVarName}.Expressions = new List<ExpressionInfo>();");
                
                string exprVarName = $"expr{System.Guid.NewGuid().ToString().Replace("-", "")}";
                source.Append(ASTExpressionGenerator.GenerateAST(exprStmt.Expression, indent, semanticModel, exprVarName));
                source.AppendLine($"{indent}{statementVarName}.Expressions.Add({exprVarName});");
            }
            // Add more specific handlers as needed for other statement types
            
            source.AppendLine($"{indent}methodBodyInfo.Statements.Add({statementVarName});");
            
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
            if (statement is ExpressionStatementSyntax) return "Expression";
            if (statement is ReturnStatementSyntax) return "Return";
            // Add more statement types as needed
            
            return "Unknown";
        }
    }
}