// This file contains reflection-based AST models for method bodies and expressions
// that complement standard reflection with syntax information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace UnityGLTF.Interactivity.AST
{
    /// <summary>
    /// Represents a class with detailed reflection information and syntax details
    /// </summary>
    public class ClassReflectionInfo
    {
        /// <summary>
        /// The actual Type of the class
        /// </summary>
        public Type Type { get; set; }
        
        /// <summary>
        /// Class modifiers (public, abstract, sealed, etc.)
        /// </summary>
        public List<string> Modifiers { get; set; } = new List<string>();
        
        /// <summary>
        /// Base types and interfaces this class inherits or implements
        /// </summary>
        public List<Type> BaseTypes { get; set; } = new List<Type>();
        
        /// <summary>
        /// Fields in this class
        /// </summary>
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
        
        /// <summary>
        /// Properties in this class
        /// </summary>
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();
        
        /// <summary>
        /// Methods in this class
        /// </summary>
        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();
        
        /// <summary>
        /// Method bodies with detailed information
        /// </summary>
        public Dictionary<string, MethodBodyInfo> MethodBodies { get; set; } = new Dictionary<string, MethodBodyInfo>();
    }
    
    /// <summary>
    /// Represents a method body with detailed statement and expression information
    /// </summary>
    public class MethodBodyInfo
    {
        /// <summary>
        /// The actual MethodInfo from reflection this body belongs to
        /// </summary>
        public MethodInfo Method { get; set; }
        
        /// <summary>
        /// Statements in the method body
        /// </summary>
        public List<StatementInfo> Statements { get; set; } = new List<StatementInfo>();
        
        /// <summary>
        /// Local variables in the method body
        /// </summary>
        public Dictionary<string, Type> LocalVariables { get; set; } = new Dictionary<string, Type>();
    }
    
    /// <summary>
    /// Represents a statement in code with reflection-based types
    /// </summary>
    public class StatementInfo
    {
        public enum StatementKind
        {
            Unknown,
            Block,
            If,
            For,
            ForEach,
            While,
            Do,
            Switch,
            Return,
            Expression,
            Declaration,
            Assignment,
            Break,
            Continue,
            Throw,
            Try,
            Catch,
            Finally,
            Class,
            Field,
            Property,
            AccessorBlock,
            AccessorExpression,
            Method,
            Parameter,
            ExpressionBody,
            ExpressionBodyExpression,
            Condition,
            ForInitializer,
            ForCondition,
            ForIncrementor,
            WhileCondition,
            MethodCall,
            MemberAccess,
            BinaryExpression,
            IfStatement,
            ThenClause,
            ElseClause,
            ForStatement,
            VariableDeclarator,
            WhileStatement,
            ExpressionStatement,
            ReturnStatement,
            LocalDeclaration,
            Initializer,
            InvocationExpression,
            ObjectCreationExpression,
            PropertyAccessExpression,
            Literal,
            Identifier,
            UnknownExpression,
            AssignmentExpression
        }
        
        /// <summary>
        /// Type of statement
        /// </summary>
        public StatementKind Kind { get; set; }
        
        /// <summary>
        /// Child statements (for compound statements)
        /// </summary>
        public List<StatementInfo> Children { get; set; } = new List<StatementInfo>();
        
        /// <summary>
        /// Expressions contained in this statement
        /// </summary>
        public List<ExpressionInfo> Expressions { get; set; } = new List<ExpressionInfo>();
    }
    
    /// <summary>
    /// Represents an expression with reflection-based types
    /// </summary>
    public class ExpressionInfo
    {
        public enum ExpressionKind
        {
            Unknown,
            Literal,
            Identifier,
            MemberAccess,
            MethodInvocation,
            ObjectCreation,
            Binary,
            Unary,
            Assignment,
            Conditional,
            Lambda,
            ArrayCreation,
            ArrayIndex,
            Cast
        }
        
        /// <summary>
        /// Type of expression
        /// </summary>
        public ExpressionKind Kind { get; set; }
        
        /// <summary>
        /// The actual Type of this expression's result, if known
        /// </summary>
        public Type ResultType { get; set; }
        
        /// <summary>
        /// The method being invoked if this is a method call
        /// </summary>
        public MethodInfo Method { get; set; }
        
        /// <summary>
        /// The property being accessed if this is a property access
        /// </summary>
        public PropertyInfo Property { get; set; }
        
        /// <summary>
        /// The field being accessed if this is a field access
        /// </summary>
        public FieldInfo Field { get; set; }
        
        /// <summary>
        /// Child expressions (for compound expressions)
        /// </summary>
        public List<ExpressionInfo> Children { get; set; } = new List<ExpressionInfo>();
        
        /// <summary>
        /// Literal value if this is a literal expression
        /// </summary>
        public object LiteralValue { get; set; }
        
        /// <summary>
        /// Operator type for binary expressions (e.g., "+", "-", "*", "/", etc.)
        /// </summary>
        public string Operator { get; set; }
    }
}
