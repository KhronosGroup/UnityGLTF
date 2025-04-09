// This file must be kept in sync with
// <link href="InteractivityASTGenerator~/ASTModels.cs">

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityGLTF.Interactivity.AST
{
    #region Base Node
    /// <summary>
    /// Base class for all AST nodes
    /// </summary>
    public class ASTNode
    {
        public string Kind { get; set; }
        
        /// <summary>
        /// Default indentation level for ToString formatting
        /// </summary>
        protected const int DefaultIndent = 2;
        
        /// <summary>
        /// Creates a string representation of the node with proper indentation
        /// </summary>
        /// <param name="indent">Indentation level</param>
        /// <returns>String representation of the node</returns>
        public virtual string ToString(int indent)
        {
            return $"{new string(' ', indent)}[{GetType().Name}] Kind: {Kind}";
        }
        
        /// <summary>
        /// Creates a string representation of the node using default indentation
        /// </summary>
        /// <returns>String representation of the node</returns>
        public override string ToString()
        {
            return ToString(0);
        }
        
        /// <summary>
        /// Creates indentation for the current level
        /// </summary>
        protected string GetIndent(int level)
        {
            return new string(' ', level);
        }
        
        /// <summary>
        /// Format a list of items with proper indentation
        /// </summary>
        protected string FormatList<T>(IEnumerable<T> list, int indent, Func<T, int, string> formatter = null)
        {
            if (list == null || !list.GetEnumerator().MoveNext())
                return "[]";
                
            var sb = new StringBuilder();
            sb.AppendLine("[");
            
            var index = 0;
            foreach (var item in list)
            {
                if (formatter != null)
                    sb.Append(formatter(item, indent + DefaultIndent));
                else if (item is ASTNode node)
                    sb.Append(node.ToString(indent + DefaultIndent));
                else
                    sb.Append($"{GetIndent(indent + DefaultIndent)}{item}");
                    
                if (++index < ((list as ICollection<T>)?.Count ?? int.MaxValue))
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }
            
            sb.Append($"{GetIndent(indent)}]");
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            
            sb.AppendLine($"{GetIndent(indent)}Modifiers: {string.Join(", ", Modifiers)}");
            sb.AppendLine($"{GetIndent(indent)}BaseTypes: {string.Join(", ", BaseTypes)}");
            
            sb.AppendLine($"{GetIndent(indent)}Fields: {FormatList(Fields, indent)}");
            sb.AppendLine($"{GetIndent(indent)}Properties: {FormatList(Properties, indent)}");
            sb.AppendLine($"{GetIndent(indent)}Methods: {FormatList(Methods, indent)}");
            
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(Text))
                sb.AppendLine($"{GetIndent(indent)}Text: {Text}");
                
            if (ExpressionType != null)
            {
                sb.AppendLine($"{GetIndent(indent)}ExpressionType:");
                sb.AppendLine(ExpressionType.ToString(indent + DefaultIndent));
            }
            
            if (ChildExpressions.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}ChildExpressions: {FormatList(ChildExpressions, indent)}");
                
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents an object creation expression in the AST
    /// </summary>
    public class ASTObjectCreationExpression : ASTExpression
    {
        public ASTTypeInfo CreatedType { get; set; }
        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();
        public List<ASTExpression> Initializers { get; set; } = new List<ASTExpression>();
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            
            if (CreatedType != null)
            {
                sb.AppendLine($"{GetIndent(indent)}CreatedType:");
                sb.AppendLine(CreatedType.ToString(indent + DefaultIndent));
            }
            
            if (Arguments.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Arguments: {FormatList(Arguments, indent)}");
                
            if (Initializers.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Initializers: {FormatList(Initializers, indent)}");
                
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Represents a method invocation expression in the AST
    /// </summary>
    public class ASTInvocationExpression : ASTExpression
    {
        public ASTMethodInfo MethodInfo { get; set; }
        public ASTExpression TargetExpression { get; set; }
        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            
            if (MethodInfo != null)
            {
                sb.AppendLine($"{GetIndent(indent)}MethodInfo:");
                sb.AppendLine(MethodInfo.ToString(indent + DefaultIndent));
            }
            
            if (TargetExpression != null)
            {
                sb.AppendLine($"{GetIndent(indent)}TargetExpression:");
                sb.AppendLine(TargetExpression.ToString(indent + DefaultIndent));
            }
            
            if (Arguments.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Arguments: {FormatList(Arguments, indent)}");
                
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Represents a property access expression (e.g., transform.position) in the AST
    /// </summary>
    public class ASTPropertyAccessExpression : ASTExpression
    {
        public ASTExpression Expression { get; set; } // The expression being accessed (e.g., 'transform')
        public string MemberName { get; set; } // The name of the member being accessed (e.g., 'position')
        public ASTTypeInfo MemberType { get; set; } // Type information about the member
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            
            if (Expression != null)
            {
                sb.AppendLine($"{GetIndent(indent)}Expression:");
                sb.AppendLine(Expression.ToString(indent + DefaultIndent));
            }
            
            sb.AppendLine($"{GetIndent(indent)}MemberName: {MemberName}");
            
            if (MemberType != null)
            {
                sb.AppendLine($"{GetIndent(indent)}MemberType:");
                sb.AppendLine(MemberType.ToString(indent + DefaultIndent));
            }
            
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            sb.AppendLine($"{GetIndent(indent)}Type: {Type}");
            sb.AppendLine($"{GetIndent(indent)}Modifiers: {string.Join(", ", Modifiers)}");
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            sb.AppendLine($"{GetIndent(indent)}Type: {Type}");
            sb.AppendLine($"{GetIndent(indent)}Modifiers: {string.Join(", ", Modifiers)}");
            sb.AppendLine($"{GetIndent(indent)}Accessors: {string.Join(", ", Accessors)}");
            
            if (AccessorBodies.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}AccessorBodies: {FormatList(AccessorBodies, indent)}");
                
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            sb.AppendLine($"{GetIndent(indent)}ReturnType: {ReturnType}");
            sb.AppendLine($"{GetIndent(indent)}Modifiers: {string.Join(", ", Modifiers)}");
            
            if (Parameters.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Parameters: {FormatList(Parameters, indent)}");
                
            if (Body.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Body: {FormatList(Body, indent)}");
                
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a parameter in the AST
    /// </summary>
    public class ASTParameter : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            sb.AppendLine($"{GetIndent(indent)}Type: {Type}");
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(Text))
                sb.AppendLine($"{GetIndent(indent)}Text: {Text}");
                
            if (ChildStatements.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}ChildStatements: {FormatList(ChildStatements, indent)}");
                
            if (Expressions.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Expressions: {FormatList(Expressions, indent)}");
                
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Represents a block of statements in the AST
    /// </summary>
    public class ASTBlockStatement : ASTStatement
    {
        public List<ASTStatement> Statements { get; set; } = new List<ASTStatement>();
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            
            if (Statements.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Statements: {FormatList(Statements, indent)}");
                
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}TypeName: {TypeName}");
            
            if (!string.IsNullOrEmpty(Namespace))
                sb.AppendLine($"{GetIndent(indent)}Namespace: {Namespace}");
                
            sb.AppendLine($"{GetIndent(indent)}IsGeneric: {IsGeneric}");
            
            if (TypeArguments.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}TypeArguments: {FormatList(TypeArguments, indent)}");
                
            return sb.ToString();
        }
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
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            sb.AppendLine($"{GetIndent(indent)}IsConstructor: {IsConstructor}");
            
            if (ReturnType != null)
            {
                sb.AppendLine($"{GetIndent(indent)}ReturnType:");
                sb.AppendLine(ReturnType.ToString(indent + DefaultIndent));
            }
            
            if (DeclaringType != null)
            {
                sb.AppendLine($"{GetIndent(indent)}DeclaringType:");
                sb.AppendLine(DeclaringType.ToString(indent + DefaultIndent));
            }
            
            if (Parameters.Count > 0)
                sb.AppendLine($"{GetIndent(indent)}Parameters: {FormatList(Parameters, indent)}");
                
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Represents parameter information in the AST
    /// </summary>
    public class ASTParameterInfo : ASTNode
    {
        public string Name { get; set; }
        public ASTTypeInfo ParameterType { get; set; }
        
        public override string ToString(int indent)
        {
            var sb = new StringBuilder(base.ToString(indent));
            sb.AppendLine();
            sb.AppendLine($"{GetIndent(indent)}Name: {Name}");
            
            if (ParameterType != null)
            {
                sb.AppendLine($"{GetIndent(indent)}ParameterType:");
                sb.AppendLine(ParameterType.ToString(indent + DefaultIndent));
            }
            
            return sb.ToString();
        }
    }
    #endregion
}