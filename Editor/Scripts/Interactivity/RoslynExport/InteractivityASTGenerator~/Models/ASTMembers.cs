using System.Collections.Generic;

namespace InteractivityASTGenerator.Models
{
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
}