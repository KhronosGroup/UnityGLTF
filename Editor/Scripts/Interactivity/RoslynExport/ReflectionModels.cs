// This file contains reflection-based AST models for method bodies and expressions
// that complement standard reflection with syntax information.

using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>
        /// Returns a string representation of the class with all its members
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"Class: {Type?.FullName ?? "Unknown"} (Name: {Type?.Name ?? "Unknown"})");
            
            if (Modifiers.Count > 0)
            {
                sb.AppendLine($"  Modifiers: {string.Join(" ", Modifiers)}");
            }
            
            if (BaseTypes.Count > 0)
            {
                sb.AppendLine("  Base Types:");
                foreach (var baseType in BaseTypes)
                {
                    sb.AppendLine($"    - {baseType.FullName} (Name: {baseType.Name})");
                }
            }
            
            if (Fields.Count > 0)
            {
                sb.AppendLine("  Fields:");
                foreach (var field in Fields)
                {
                    sb.AppendLine($"    - {field.FieldType.FullName} {field.Name}");
                }
            }
            
            if (Properties.Count > 0)
            {
                sb.AppendLine("  Properties:");
                foreach (var prop in Properties)
                {
                    sb.AppendLine($"    - {prop.PropertyType.FullName} {prop.Name}");
                }
            }
            
            if (Methods.Count > 0)
            {
                sb.AppendLine("  Methods:");
                foreach (var method in Methods)
                {
                    sb.AppendLine($"    - {method.ReturnType.FullName} {method.Name}()");
                }
            }
            
            if (MethodBodies.Count > 0)
            {
                sb.AppendLine("  Method Bodies:");
                foreach (var methodBody in MethodBodies)
                {
                    sb.AppendLine($"    - {methodBody.Key}:");
                    sb.AppendLine(methodBody.Value.ToString().Replace("\n", "\n      "));
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a Mermaid flowchart representation of this class
        /// </summary>
        public string ToMermaidFlowchart()
        {
            StringBuilder sb = new StringBuilder();
            
            // No backticks as requested
            sb.AppendLine("flowchart TD");
            
            string className = Type?.Name ?? "UnknownClass";
            string classFullName = Type?.FullName ?? "Unknown";
            string classId = "c" + className.Replace(".", "_");
            
            sb.AppendLine($"  {classId}[\"{className}<br/>{classFullName}\"]");
            
            // Add methods
            foreach (var method in Methods)
            {
                string methodId = $"m_{classId}_{method.Name}";
                string returnType = method.ReturnType?.FullName ?? "void";
                string methodFullName = $"{method.Name}()<br/>Return: {returnType}";
                
                sb.AppendLine($"  {methodId}[\"{methodFullName}\"]");
                sb.AppendLine($"  {classId} --> {methodId}");
                
                if (MethodBodies.TryGetValue(method.Name, out var methodBody))
                {
                    sb.Append(methodBody.ToMermaidFlowchart(methodId));
                }
            }
            
            return sb.ToString();
        }
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
        
        /// <summary>
        /// Returns a string representation of the method body with all statements
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"Method: {Method?.DeclaringType?.FullName}.{Method?.Name ?? "Unknown"}");
            
            if (LocalVariables.Count > 0)
            {
                sb.AppendLine("  Local Variables:");
                foreach (var variable in LocalVariables)
                {
                    sb.AppendLine($"    - {variable.Value.FullName} {variable.Key}");
                }
            }
            
            if (Statements.Count > 0)
            {
                sb.AppendLine("  Statements:");
                foreach (var statement in Statements)
                {
                    sb.AppendLine(statement.ToString().Replace("\n", "\n    "));
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a Mermaid flowchart representation of this method body
        /// </summary>
        public string ToMermaidFlowchart(string parentId)
        {
            StringBuilder sb = new StringBuilder();
            
            // Add statements to flowchart
            for (int i = 0; i < Statements.Count; i++)
            {
                string statementId = $"s_{parentId}_{i}";
                string statementLabel = Statements[i].Kind.ToString();
                
                sb.AppendLine($"  {statementId}[\"{statementLabel}\"]");
                
                // Connect statements in sequence
                if (i == 0)
                {
                    sb.AppendLine($"  {parentId} --> {statementId}");
                }
                else
                {
                    string prevStatementId = $"s_{parentId}_{i-1}";
                    sb.AppendLine($"  {prevStatementId} --> {statementId}");
                }
                
                // Add nested statement details
                sb.Append(Statements[i].ToMermaidFlowchart(statementId));
            }
            
            return sb.ToString();
        }
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
        
        /// <summary>
        /// Returns a string representation of this statement and its children
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"Statement: {Kind}");
            
            if (Expressions.Count > 0)
            {
                sb.AppendLine("  Expressions:");
                foreach (var expression in Expressions)
                {
                    sb.AppendLine(expression.ToString().Replace("\n", "\n    "));
                }
            }
            
            if (Children.Count > 0)
            {
                sb.AppendLine("  Child Statements:");
                foreach (var child in Children)
                {
                    sb.AppendLine(child.ToString().Replace("\n", "\n    "));
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a Mermaid flowchart representation of this statement
        /// </summary>
        public string ToMermaidFlowchart(string parentId)
        {
            StringBuilder sb = new StringBuilder();
            
            // Add expressions to the flowchart
            for (int i = 0; i < Expressions.Count; i++)
            {
                string exprId = $"expr_{parentId}_{i}";
                
                // Build a detailed expression label with type information
                StringBuilder labelSb = new StringBuilder();
                labelSb.Append(Expressions[i].Kind.ToString());
                
                // Add type information if available
                if (Expressions[i].ResultType != null)
                {
                    labelSb.Append($"<br/>{Expressions[i].ResultType.Name}");
                }
                
                if (Expressions[i].LiteralValue != null)
                {
                    string literalStr;
                    if (Expressions[i].LiteralValue is double || Expressions[i].LiteralValue is float || Expressions[i].LiteralValue is decimal)
                    {
                        literalStr = Convert.ToString(Expressions[i].LiteralValue, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        literalStr = Expressions[i].LiteralValue.ToString();
                    }
                    // Escape quotes for mermaid diagram
                    literalStr = literalStr.Replace("\"", "'");
                    // Truncate if too long
                    if (literalStr.Length > 20)
                    {
                        literalStr = literalStr.Substring(0, 17) + "...";
                    }
                    labelSb.Append($"<br/>Value: {literalStr}");
                }
                
                string expressionLabel = labelSb.ToString();
                
                sb.AppendLine($"  {exprId}{{\" {expressionLabel} \"}}");
                sb.AppendLine($"  {parentId} --> {exprId}");
                
                // Add nested expression details
                sb.Append(Expressions[i].ToMermaidFlowchart(exprId));
            }
            
            // Add child statements with more detailed information
            for (int i = 0; i < Children.Count; i++)
            {
                string childId = $"child_{parentId}_{i}";
                string childLabel = $"{Children[i].Kind}";
                
                sb.AppendLine($"  {childId}[\"{childLabel}\"]");
                sb.AppendLine($"  {parentId} --> {childId}");
                
                // Add nested statement details
                sb.Append(Children[i].ToMermaidFlowchart(childId));
            }
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Represents an expression with reflection-based types
    /// </summary>
    public class ExpressionInfo
    {
        /// <summary>
        /// Defines the types of expressions that can be represented in the AST
        /// </summary>
        public enum ExpressionKind
        {
            /** Represents an expression whose kind could not be determined */
            Unknown,
            
            /** Represents a literal value (e.g., string, number, boolean, etc.) */
            Literal,
            
            /** Represents a name identifier in code (e.g., variable name) */
            Identifier,
            
            /** Represents member access (e.g., obj.Property) */
            MemberAccess,
            
            /** Represents a method call (e.g., Method() or obj.Method()) */
            MethodInvocation,
            
            /** Represents object creation (e.g., new Type()) */
            ObjectCreation,
            
            /** Represents a binary operation (e.g., a + b, a == b) */
            Binary,
            
            /** Represents a unary operation (e.g., !x, ~x) */
            Unary,
            
            /** Represents an assignment (e.g., a = b, a += b) */
            Assignment,
            
            /** Represents a conditional expression (e.g., condition ? ifTrue : ifFalse) */
            Conditional,
            
            /** Represents a lambda expression (e.g., () => expression or x => x * 2) */
            Lambda,
            
            /** Represents array creation (e.g., new int[] { 1, 2, 3 }) */
            ArrayCreation,
            
            /** Represents array indexing (e.g., array[0]) */
            ArrayIndex,
            
            /** Represents a type cast (e.g., (int)value) */
            Cast,
            
            /** Represents multiplication (e.g., a * b) */
            Multiplication,
            
            /** Represents addition (e.g., a + b) */
            Addition,
            
            /** Represents subtraction (e.g., a - b) */
            Subtraction,
            
            /** Represents division (e.g., a / b) */
            Division,
            
            /** Represents a method argument */
            MethodArgument,
            
            /** Represents a for loop initializer */
            ForInitializer,
            
            /** Represents a for loop condition */
            ForCondition,
            
            /** Represents a for loop incrementor */
            ForIncrementor,
            
            /** Represents postfix increment/decrement (e.g., i++, i--) */
            PostfixUnary,
            
            /** Represents prefix increment/decrement (e.g., ++i, --i) */
            PrefixUnary,
            
            /** Represents a switch expression (e.g., x switch { 1 => "one", _ => "other" }) */
            SwitchExpression,
            
            /** Represents a switch arm (e.g., 1 => "one") */
            SwitchArm,
            
            /** Represents a switch pattern (e.g., 1 or > 0 or _ or Person { Age: > 18 }) */
            SwitchPattern,
            
            /** Represents an await expression (e.g., await task) */
            AwaitExpression,
            
            /** Represents an async lambda (e.g., async () => await task) */
            AsyncLambda,
            
            /** Represents a throw expression (e.g., throw new Exception()) */
            ThrowExpression,
            
            /** Represents pattern matching (e.g., is pattern expressions) */
            PatternMatching,
            
            /** Represents array/list indexing (e.g., array[index]) */
            ElementAccess,
            
            /** Represents expressions in parentheses (e.g., (a + b)) */
            Parenthesized,
            
            /** Represents string interpolation (e.g., $"Hello {name}") */
            InterpolatedString,
            
            /** Represents anonymous object creation (e.g., new { Name = "value" }) */
            AnonymousObject,
            
            /** Represents default value expressions (e.g., default(int) or default) */
            DefaultValue,
            
            /** Represents typeof expressions (e.g., typeof(int)) */
            TypeOf,
            
            /** Represents nameof expressions (e.g., nameof(variable)) */
            NameOf,
            
            /** Represents null coalescing operator (e.g., a ?? b) */
            NullCoalescing,
            
            /** Represents null conditional operator (e.g., a?.b) */
            NullConditional,
            
            /** Represents range expressions (e.g., 1..5) */
            RangeExpression,
            
            /** Represents is pattern matching (e.g., x is int i) */
            IsPattern,
            
            /** Represents type testing with as (e.g., obj as string) */
            AsExpression,
            
            /** Represents LINQ query expressions (e.g., from x in list where x > 0 select x) */
            QueryExpression,
            
            /** Represents object or collection initializer expressions (e.g., new List<int> { 1, 2, 3 }) */
            InitializerExpression
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
        
        /// <summary>
        /// Returns a string representation of this expression and its children
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string typeName = ResultType != null ? ResultType.FullName : "unknown";
            
            sb.Append($"Expression: {Kind} (Type: {typeName})");
            
            if (LiteralValue != null)
            {
                if (LiteralValue is double || LiteralValue is float || LiteralValue is decimal)
                {
                    sb.Append($" Value: {Convert.ToString(LiteralValue, CultureInfo.InvariantCulture)}");
                }
                else
                {
                    sb.Append($" Value: {LiteralValue}");
                }
            }
            
            if (!string.IsNullOrEmpty(Operator))
            {
                sb.Append($" Operator: {Operator}");
            }
            
            if (Method != null)
            {
                sb.Append($" Method: {Method.DeclaringType?.FullName}.{Method.Name}");
            }
            
            if (Property != null)
            {
                sb.Append($" Property: {Property.DeclaringType?.FullName}.{Property.Name}");
            }
            
            if (Field != null)
            {
                sb.Append($" Field: {Field.DeclaringType?.FullName}.{Field.Name}");
            }
            
            sb.AppendLine();
            
            if (Children.Count > 0)
            {
                sb.AppendLine("  Child Expressions:");
                foreach (var child in Children)
                {
                    sb.AppendLine(child.ToString().Replace("\n", "\n    "));
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a Mermaid flowchart representation of this expression
        /// </summary>
        public string ToMermaidFlowchart(string parentId)
        {
            StringBuilder sb = new StringBuilder();
            
            // Add child expressions to the flowchart
            for (int i = 0; i < Children.Count; i++)
            {
                string childId = $"expr_child_{parentId}_{i}";
                
                // Create a detailed multiline label with full type information
                StringBuilder labelSb = new StringBuilder();
                
                // Start with expression kind
                labelSb.Append(Children[i].Kind.ToString());
                
                // Add type information if available
                if (Children[i].ResultType != null)
                {
                    string typeName = Children[i].ResultType.Name;
                    labelSb.Append($"<br/>Type: {typeName}");
                }
                
                // Add literal value
                if (Children[i].LiteralValue != null)
                {
                    string literalStr;
                    if (Children[i].LiteralValue is double || Children[i].LiteralValue is float || Children[i].LiteralValue is decimal)
                    {
                        literalStr = Convert.ToString(Children[i].LiteralValue, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        literalStr = Children[i].LiteralValue.ToString();
                    }
                    // Escape quotes for mermaid diagram
                    literalStr = literalStr.Replace("\"", "'");
                    // Truncate if too long
                    if (literalStr.Length > 20)
                    {
                        literalStr = literalStr.Substring(0, 17) + "...";
                    }
                    labelSb.Append($"<br/>Value: {literalStr}");
                }
                
                // Add operator information
                if (!string.IsNullOrEmpty(Children[i].Operator))
                {
                    labelSb.Append($"<br/>Op: {Children[i].Operator}");
                }
                
                // Add method information
                if (Children[i].Method != null)
                {
                    string methodName = Children[i].Method.Name;
                    labelSb.Append($"<br/>Method: {methodName}()");
                }
                
                // Add property information
                if (Children[i].Property != null)
                {
                    string propertyName = Children[i].Property.Name;
                    labelSb.Append($"<br/>Property: {propertyName}");
                }
                
                // Add field information
                if (Children[i].Field != null)
                {
                    string fieldName = Children[i].Field.Name;
                    labelSb.Append($"<br/>Field: {fieldName}");
                }
                
                string expressionLabel = labelSb.ToString();
                
                sb.AppendLine($"  {childId}{{\" {expressionLabel} \"}}");
                sb.AppendLine($"  {parentId} --> {childId}");
                
                // Add nested expression details recursively
                sb.Append(Children[i].ToMermaidFlowchart(childId));
            }
            
            return sb.ToString();
        }
    }
}