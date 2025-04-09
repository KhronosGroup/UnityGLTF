using System.Collections.Generic;

namespace InteractivityASTGenerator.Models
{
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
}