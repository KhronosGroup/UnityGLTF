using System.Collections.Generic;

namespace InteractivityASTGenerator.Models
{
    /// <summary>
    /// Represents a statement in the AST
    /// </summary>
    public class ASTStatement : ASTNode
    {
        public string Text { get; set; }
        public List<ASTStatement> ChildStatements { get; set; } = new List<ASTStatement>();
        public List<ASTExpression> Expressions { get; set; } = new List<ASTExpression>();
    }
    
    /// <summary>
    /// Represents a block of statements in the AST
    /// </summary>
    public class ASTBlockStatement : ASTStatement
    {
        public List<ASTStatement> Statements { get; set; } = new List<ASTStatement>();
    }
}