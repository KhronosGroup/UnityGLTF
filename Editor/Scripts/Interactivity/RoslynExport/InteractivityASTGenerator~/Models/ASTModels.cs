using System;
using System.Collections.Generic;

namespace InteractivityASTGenerator.Models
{
    #region Base Node
    /// <summary>
    /// Base class for all AST nodes
    /// </summary>
    public class ASTNode
    {
        public string Kind { get; set; }
    }
    #endregion

    #region Class
    /// <summary>
    /// Represents a class in the AST
    /// </summary>
    public class ASTClass : ASTNode
    {
        public string Name { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<string> BaseTypes { get; set; } = new List<string>();
        public List<ASTField> Fields { get; set; } = new List<ASTField>();
        public List<ASTProperty> Properties { get; set; } = new List<ASTProperty>();
        public List<ASTMethod> Methods { get; set; } = new List<ASTMethod>();
    }
    #endregion

    #region Expressions
    /// <summary>
    /// Base class for expressions in the AST
    /// </summary>
    public class ASTExpression : ASTNode
    {
        public string Text { get; set; }
        public ASTTypeInfo ExpressionType { get; set; }
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
    
    /// <summary>
    /// Represents a property access expression (e.g., transform.position) in the AST
    /// </summary>
    public class ASTPropertyAccessExpression : ASTExpression
    {
        public ASTExpression Expression { get; set; } // The expression being accessed (e.g., 'transform')
        public string MemberName { get; set; } // The name of the member being accessed (e.g., 'position')
        public ASTTypeInfo MemberType { get; set; } // Type information about the member
    }
    #endregion

    #region Members
    /// <summary>
    /// Represents a field in the AST
    /// </summary>
    public class ASTField : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a property in the AST
    /// </summary>
    public class ASTProperty : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<string> Accessors { get; set; } = new List<string>();
        public List<ASTStatement> AccessorBodies { get; set; } = new List<ASTStatement>();
    }

    /// <summary>
    /// Represents a method in the AST
    /// </summary>
    public class ASTMethod : ASTNode
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<ASTParameter> Parameters { get; set; } = new List<ASTParameter>();
        public List<ASTStatement> Body { get; set; } = new List<ASTStatement>();
    }

    /// <summary>
    /// Represents a parameter in the AST
    /// </summary>
    public class ASTParameter : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    #endregion

    #region Statements
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
    #endregion

    #region Type Information
    /// <summary>
    /// Represents type information in the AST
    /// </summary>
    public class ASTTypeInfo : ASTNode
    {
        public string TypeName { get; set; }
        public string Namespace { get; set; }
        public bool IsGeneric { get; set; }
        public List<ASTTypeInfo> TypeArguments { get; set; } = new List<ASTTypeInfo>();
    }
    
    /// <summary>
    /// Represents method information in the AST
    /// </summary>
    public class ASTMethodInfo : ASTNode
    {
        public string Name { get; set; }
        public ASTTypeInfo ReturnType { get; set; }
        public ASTTypeInfo DeclaringType { get; set; }
        public List<ASTParameterInfo> Parameters { get; set; } = new List<ASTParameterInfo>();
        public bool IsConstructor { get; set; }
    }
    
    /// <summary>
    /// Represents parameter information in the AST
    /// </summary>
    public class ASTParameterInfo : ASTNode
    {
        public string Name { get; set; }
        public ASTTypeInfo ParameterType { get; set; }
    }
    #endregion
}