using System.Collections.Generic;

namespace InteractivityASTGenerator.Models
{
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
}