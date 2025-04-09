using System.Collections.Generic;

namespace InteractivityASTGenerator.Models
{
    /// <summary>
    /// Base class for expressions in the AST
    /// </summary>
    public class ASTExpression : ASTNode
    {
        public string Text { get; set; }
        public string ExpressionType { get; set; }
        public List<ASTExpression> ChildExpressions { get; set; } = new List<ASTExpression>();
    }

    /// <summary>
    /// Represents an object creation expression in the AST
    /// </summary>
    public class ASTObjectCreationExpression : ASTExpression
    {
        public ASTTypeInfo CreatedType { get; set; }
        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();
        public List<ASTExpression> Initializers { get; set; } = new List<ASTExpression>();
    }
    
    /// <summary>
    /// Represents a method invocation expression in the AST
    /// </summary>
    public class ASTInvocationExpression : ASTExpression
    {
        public ASTMethodInfo MethodInfo { get; set; }
        public ASTExpression TargetExpression { get; set; }
        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();
    }
}