using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InteractivityASTGenerator
{
    // ASTNode class hierarchy to represent the syntax tree
    public class ASTNode
    {
        public string Kind { get; set; }
    }

    public class ASTClass : ASTNode
    {
        public string Name { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<string> BaseTypes { get; set; } = new List<string>();
        public List<ASTField> Fields { get; set; } = new List<ASTField>();
        public List<ASTProperty> Properties { get; set; } = new List<ASTProperty>();
        public List<ASTMethod> Methods { get; set; } = new List<ASTMethod>();
    }

    public class ASTField : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }

    public class ASTProperty : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<string> Accessors { get; set; } = new List<string>();
        public List<ASTStatement> AccessorBodies { get; set; } = new List<ASTStatement>();
    }

    public class ASTMethod : ASTNode
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<ASTParameter> Parameters { get; set; } = new List<ASTParameter>();
        public List<ASTStatement> Body { get; set; } = new List<ASTStatement>();
    }

    public class ASTParameter : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    
    public class ASTStatement : ASTNode
    {
        public string Text { get; set; }
        public List<ASTStatement> ChildStatements { get; set; } = new List<ASTStatement>();
        public List<ASTExpression> Expressions { get; set; } = new List<ASTExpression>();
    }
    
    public class ASTBlockStatement : ASTStatement
    {
        public List<ASTStatement> Statements { get; set; } = new List<ASTStatement>();
    }
    
    public class ASTExpression : ASTNode
    {
        public string Text { get; set; }
        public string ExpressionType { get; set; }
        public List<ASTExpression> ChildExpressions { get; set; } = new List<ASTExpression>();
    }

    public class ASTTypeInfo : ASTNode
    {
        public string TypeName { get; set; }
        public string Namespace { get; set; }
        public bool IsGeneric { get; set; }
        public List<ASTTypeInfo> TypeArguments { get; set; } = new List<ASTTypeInfo>();
    }
    
    public class ASTMethodInfo : ASTNode
    {
        public string Name { get; set; }
        public ASTTypeInfo ReturnType { get; set; }
        public ASTTypeInfo DeclaringType { get; set; }
        public List<ASTParameterInfo> Parameters { get; set; } = new List<ASTParameterInfo>();
        public bool IsConstructor { get; set; }
    }
    
    public class ASTParameterInfo : ASTNode
    {
        public string Name { get; set; }
        public ASTTypeInfo ParameterType { get; set; }
    }
    
    public class ASTObjectCreationExpression : ASTExpression
    {
        public ASTTypeInfo CreatedType { get; set; }
        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();
        public List<ASTExpression> Initializers { get; set; } = new List<ASTExpression>();
    }
    
    public class ASTInvocationExpression : ASTExpression
    {
        public ASTMethodInfo MethodInfo { get; set; }
        public ASTExpression TargetExpression { get; set; }
        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();
    }

    [Generator]
    public class GLTFInteractivitySourceGenerator : ISourceGenerator
    {
        private const string AttributeName = "GLTFInteractivityCompile";
        private const string IgnoreAttributeName = "GLTFInteractivityIgnore";
        private const string LogCategory = "GLTFInteractivityGenerator";
        
        // Diagnostic IDs
        private const string InfoDiagnosticId = "GLTFGEN01";
        private const string ErrorDiagnosticId = "GLTFGEN99";
        private const string VerboseDiagnosticId = "GLTFGEN02";
        
        // Diagnostic descriptors
        private static readonly DiagnosticDescriptor InfoDescriptor = new DiagnosticDescriptor(
            id: InfoDiagnosticId,
            title: "GLTF Generator Info",
            messageFormat: "{0}",
            category: LogCategory,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
            
        private static readonly DiagnosticDescriptor VerboseDescriptor = new DiagnosticDescriptor(
            id: VerboseDiagnosticId,
            title: "GLTF Generator Verbose",
            messageFormat: "{0}",
            category: LogCategory,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true);
            
        private static readonly DiagnosticDescriptor ErrorDescriptor = new DiagnosticDescriptor(
            id: ErrorDiagnosticId,
            title: "GLTF Generator Error",
            messageFormat: "{0}",
            category: LogCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        // Static cache to track which AST namespaces have already been generated
        private static readonly HashSet<string> GeneratedASTNamespaces = new HashSet<string>();

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                // Log execution started
                LogInfo(context, $"Source generator execution started at {DateTime.Now}");
                
                // Clear the namespace cache at the start of each execution
                GeneratedASTNamespaces.Clear();
                
                // Nothing to do if we don't have a syntax receiver
                if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                {
                    LogInfo(context, "No syntax receiver found. Exiting.");
                    return;
                }

                // Get the compilation
                Compilation compilation = context.Compilation;
                LogInfo(context, $"Found {receiver.CandidateClasses.Count} candidate classes");
                
                // First, collect all the namespaces we'll need to generate AST classes for
                var requiredASTNamespaces = new HashSet<string>();
                
                foreach (ClassDeclarationSyntax classDeclaration in receiver.CandidateClasses)
                {
                    // Get the semantic model for this class declaration
                    SemanticModel model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                    
                    // Get the class symbol
                    INamedTypeSymbol? classSymbol = model.GetDeclaredSymbol(classDeclaration);
                    
                    if (classSymbol == null)
                    {
                        LogInfo(context, $"Could not get symbol for class: {classDeclaration.Identifier.Text}");
                        continue;
                    }

                    // Check if the class has the GLTFInteractivity attribute
                    bool hasAttribute = HasGLTFInteractivityAttribute(classSymbol);
                    
                    if (!hasAttribute)
                        continue;
                    
                    // Get the namespace this class needs its AST classes in
                    string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                    bool isGlobalNamespace = string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>";
                    string astNamespace = isGlobalNamespace 
                        ? "GLTFInteractivity.AST" 
                        : $"{namespaceName}.GLTFInteractivity.AST";
                        
                    requiredASTNamespaces.Add(astNamespace);
                }
                
                // Generate AST class definitions for each required namespace
                foreach (string astNamespace in requiredASTNamespaces)
                {
                    // Generate AST classes for this namespace
                    string astClassesSource = GenerateASTClasses(astNamespace);
                    string fileName = $"ASTClasses_{astNamespace.Replace(".", "_")}.cs";
                    
                    // Add to compilation
                    context.AddSource(fileName, SourceText.From(astClassesSource, Encoding.UTF8));
                    LogInfo(context, $"Generated AST classes for namespace: {astNamespace}");
                    
                    // Mark this namespace as generated
                    GeneratedASTNamespaces.Add(astNamespace);
                }

                // Now process each class to generate the GetAST method
                foreach (ClassDeclarationSyntax classDeclaration in receiver.CandidateClasses)
                {
                    // Get the semantic model for this class declaration
                    SemanticModel model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                    
                    // Get the class symbol
                    INamedTypeSymbol? classSymbol = model.GetDeclaredSymbol(classDeclaration);
                    
                    if (classSymbol == null)
                    {
                        LogInfo(context, $"Could not get symbol for class: {classDeclaration.Identifier.Text}");
                        continue;
                    }

                    // Check if the class has the GLTFInteractivity attribute
                    bool hasAttribute = HasGLTFInteractivityAttribute(classSymbol);
                    
                    if (!hasAttribute)
                        continue;

                    // Log some details about the class
                    LogInfo(context, $"Class namespace: {classSymbol.ContainingNamespace}");
                    
                    // Generate the GetAST method
                    string classSource = GenerateGetASTMethod(classDeclaration, classSymbol);
                    string fileName = $"{classSymbol.Name}_AST.cs";
                    
                    // Add to compilation
                    context.AddSource(fileName, SourceText.From(classSource, Encoding.UTF8));
                    LogInfo(context, $"Added source file: {fileName}");
                    LogInfo(context, $"Full source:\n{classSource}");
                }
                
                LogInfo(context, $"Source generator execution completed at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                // Report exception as diagnostic
                LogError(context, $"Exception in generator: {ex}");
            }
        }

        private void LogInfo(GeneratorExecutionContext context, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(InfoDescriptor, Location.None, message));
        }

        private void LogError(GeneratorExecutionContext context, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(ErrorDescriptor, Location.None, message));
        }

        private bool HasGLTFInteractivityAttribute(INamedTypeSymbol classSymbol)
        {
            // Check if any attribute's name contains "GLTFInteractivity"
            return classSymbol.GetAttributes().Any(attr => 
                attr.AttributeClass?.Name.Contains(AttributeName) == true);
        }
        
        /// <summary>
        /// Generate AST classes for a specific namespace
        /// </summary>
        private string GenerateASTClasses(string astNamespace)
        {
            var source = new StringBuilder();
            
            // Add necessary usings
            source.AppendLine("// <auto-generated/>");
            source.AppendLine("using System;");
            source.AppendLine("using System.Collections.Generic;");
            source.AppendLine("using System.Text;");
            source.AppendLine();
            
            // Add the AST classes in their own namespace
            source.AppendLine($"namespace {astNamespace}");
            source.AppendLine("{");
            source.AppendLine("    // ASTNode class hierarchy to represent the syntax tree");
            source.AppendLine("    public class ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Kind { get; set; }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTClass : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public List<string> Modifiers { get; set; } = new List<string>();");
            source.AppendLine("        public List<string> BaseTypes { get; set; } = new List<string>();");
            source.AppendLine("        public List<ASTField> Fields { get; set; } = new List<ASTField>();");
            source.AppendLine("        public List<ASTProperty> Properties { get; set; } = new List<ASTProperty>();");
            source.AppendLine("        public List<ASTMethod> Methods { get; set; } = new List<ASTMethod>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTField : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public string Type { get; set; }");
            source.AppendLine("        public List<string> Modifiers { get; set; } = new List<string>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTProperty : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public string Type { get; set; }");
            source.AppendLine("        public List<string> Modifiers { get; set; } = new List<string>();");
            source.AppendLine("        public List<string> Accessors { get; set; } = new List<string>();");
            source.AppendLine("        public List<ASTStatement> AccessorBodies { get; set; } = new List<ASTStatement>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTMethod : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public string ReturnType { get; set; }");
            source.AppendLine("        public List<string> Modifiers { get; set; } = new List<string>();");
            source.AppendLine("        public List<ASTParameter> Parameters { get; set; } = new List<ASTParameter>();");
            source.AppendLine("        public List<ASTStatement> Body { get; set; } = new List<ASTStatement>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTParameter : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public string Type { get; set; }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTStatement : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Text { get; set; }");
            source.AppendLine("        public List<ASTStatement> ChildStatements { get; set; } = new List<ASTStatement>();");
            source.AppendLine("        public List<ASTExpression> Expressions { get; set; } = new List<ASTExpression>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTBlockStatement : ASTStatement");
            source.AppendLine("    {");
            source.AppendLine("        public List<ASTStatement> Statements { get; set; } = new List<ASTStatement>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTExpression : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Text { get; set; }");
            source.AppendLine("        public string ExpressionType { get; set; }");
            source.AppendLine("        public List<ASTExpression> ChildExpressions { get; set; } = new List<ASTExpression>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTTypeInfo : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string TypeName { get; set; }");
            source.AppendLine("        public string Namespace { get; set; }");
            source.AppendLine("        public bool IsGeneric { get; set; }");
            source.AppendLine("        public List<ASTTypeInfo> TypeArguments { get; set; } = new List<ASTTypeInfo>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTMethodInfo : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public ASTTypeInfo ReturnType { get; set; }");
            source.AppendLine("        public ASTTypeInfo DeclaringType { get; set; }");
            source.AppendLine("        public List<ASTParameterInfo> Parameters { get; set; } = new List<ASTParameterInfo>();");
            source.AppendLine("        public bool IsConstructor { get; set; }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTParameterInfo : ASTNode");
            source.AppendLine("    {");
            source.AppendLine("        public string Name { get; set; }");
            source.AppendLine("        public ASTTypeInfo ParameterType { get; set; }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTObjectCreationExpression : ASTExpression");
            source.AppendLine("    {");
            source.AppendLine("        public ASTTypeInfo CreatedType { get; set; }");
            source.AppendLine("        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();");
            source.AppendLine("        public List<ASTExpression> Initializers { get; set; } = new List<ASTExpression>();");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public class ASTInvocationExpression : ASTExpression");
            source.AppendLine("    {");
            source.AppendLine("        public ASTMethodInfo MethodInfo { get; set; }");
            source.AppendLine("        public ASTExpression TargetExpression { get; set; }");
            source.AppendLine("        public List<ASTExpression> Arguments { get; set; } = new List<ASTExpression>();");
            source.AppendLine("    }");
            source.AppendLine("}");
            
            return source.ToString();
        }

        /// <summary>
        /// Generate the GetAST method for a class
        /// </summary>
        private string GenerateGetASTMethod(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            bool isGlobalNamespace = string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>";
            string astNamespace = isGlobalNamespace 
                ? "GLTFInteractivity.AST" 
                : $"{namespaceName}.GLTFInteractivity.AST";
            
            var source = new StringBuilder();
            
            // Add necessary usings
            source.AppendLine("// <auto-generated/>");
            source.AppendLine("using System;");
            source.AppendLine("using System.Collections.Generic;");
            source.AppendLine("using System.Text;");
            source.AppendLine($"using {astNamespace};");  // Reference to our AST namespace
            source.AppendLine();
            
            // Begin namespace for actual class (only if not global)
            if (!isGlobalNamespace)
            {
                source.AppendLine($"namespace {namespaceName}");
                source.AppendLine("{");
            }
            
            string indent = isGlobalNamespace ? "" : "    ";
            
            // Create partial class with appropriate indentation
            source.AppendLine($"{indent}public partial class {classSymbol.Name}");
            source.AppendLine($"{indent}{{");

            // Add GetAST method that returns the ASTNode structure
            source.AppendLine($"{indent}    /// <summary>");
            source.AppendLine($"{indent}    /// Returns the Abstract Syntax Tree representation of this class.");
            source.AppendLine($"{indent}    /// </summary>");
            source.AppendLine($"{indent}    /// <returns>An ASTClass object representing the syntax tree.</returns>");
            source.AppendLine($"{indent}    public static {astNamespace}.ASTClass GetAST()");
            source.AppendLine($"{indent}    {{");
            source.AppendLine($"{indent}        var ast = new {astNamespace}.ASTClass");
            source.AppendLine($"{indent}        {{");
            source.AppendLine($"{indent}            Kind = \"Class\",");
            source.AppendLine($"{indent}            Name = \"{classSymbol.Name}\",");
            source.AppendLine($"{indent}            Modifiers = new List<string> {{ {string.Join(", ", classDeclaration.Modifiers.Select(m => $"\"{m.Text}\""))} }},");
            source.AppendLine($"{indent}            BaseTypes = new List<string> {{ {(classDeclaration.BaseList != null ? string.Join(", ", classDeclaration.BaseList.Types.Select(t => $"\"{t.Type}\"")) : "")} }},");
            
            // Add fields
            source.AppendLine($"{indent}            Fields = new List<{astNamespace}.ASTField>");
            source.AppendLine($"{indent}            {{");
            
            foreach (var field in classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    source.AppendLine($"{indent}                new {astNamespace}.ASTField");
                    source.AppendLine($"{indent}                {{");
                    source.AppendLine($"{indent}                    Kind = \"Field\",");
                    source.AppendLine($"{indent}                    Name = \"{variable.Identifier.Text}\",");
                    source.AppendLine($"{indent}                    Type = \"{field.Declaration.Type}\",");
                    source.AppendLine($"{indent}                    Modifiers = new List<string> {{ {string.Join(", ", field.Modifiers.Select(m => $"\"{m.Text}\""))} }}");
                    source.AppendLine($"{indent}                }},");
                }
            }
            
            source.AppendLine($"{indent}            }},");
            
            // Add properties
            source.AppendLine($"{indent}            Properties = new List<{astNamespace}.ASTProperty>");
            source.AppendLine($"{indent}            {{");
            
            foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                source.AppendLine($"{indent}                new {astNamespace}.ASTProperty");
                source.AppendLine($"{indent}                {{");
                source.AppendLine($"{indent}                    Kind = \"Property\",");
                source.AppendLine($"{indent}                    Name = \"{property.Identifier.Text}\",");
                source.AppendLine($"{indent}                    Type = \"{property.Type}\",");
                source.AppendLine($"{indent}                    Modifiers = new List<string> {{ {string.Join(", ", property.Modifiers.Select(m => $"\"{m.Text}\""))} }},");
                
                if (property.AccessorList != null)
                {
                    source.AppendLine($"{indent}                    Accessors = new List<string> {{ {string.Join(", ", property.AccessorList.Accessors.Select(a => $"\"{a.Keyword.Text}\""))} }},");
                    
                    // Add accessor bodies
                    source.AppendLine($"{indent}                    AccessorBodies = new List<{astNamespace}.ASTStatement>");
                    source.AppendLine($"{indent}                    {{");
                    
                    foreach (var accessor in property.AccessorList.Accessors)
                    {
                        if (accessor.Body != null)
                        {
                            source.AppendLine($"{indent}                        new {astNamespace}.ASTBlockStatement");
                            source.AppendLine($"{indent}                        {{");
                            source.AppendLine($"{indent}                            Kind = \"AccessorBlock\",");
                            source.AppendLine($"{indent}                            Text = @\"{accessor.Body.ToString().Replace("\"", "\"\"")}\",");
                            source.AppendLine($"{indent}                            Statements = new List<{astNamespace}.ASTStatement>()");
                            source.AppendLine($"{indent}                        }},");
                        }
                        else if (accessor.ExpressionBody != null)
                        {
                            source.AppendLine($"{indent}                        new {astNamespace}.ASTStatement");
                            source.AppendLine($"{indent}                        {{");
                            source.AppendLine($"{indent}                            Kind = \"AccessorExpression\",");
                            source.AppendLine($"{indent}                            Text = @\"{accessor.ExpressionBody.ToString().Replace("\"", "\"\"")}\"");
                            source.AppendLine($"{indent}                        }},");
                        }
                    }
                    
                    source.AppendLine($"{indent}                    }}");
                }
                else
                {
                    source.AppendLine($"{indent}                    Accessors = new List<string>(),");
                    source.AppendLine($"{indent}                    AccessorBodies = new List<{astNamespace}.ASTStatement>()");
                }
                
                source.AppendLine($"{indent}                }},");
            }
            
            source.AppendLine($"{indent}            }},");
            
            // Add methods
            source.AppendLine($"{indent}            Methods = new List<{astNamespace}.ASTMethod>");
            source.AppendLine($"{indent}            {{");
            
            foreach (var method in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                source.AppendLine($"{indent}                new {astNamespace}.ASTMethod");
                source.AppendLine($"{indent}                {{");
                source.AppendLine($"{indent}                    Kind = \"Method\",");
                source.AppendLine($"{indent}                    Name = \"{method.Identifier.Text}\",");
                source.AppendLine($"{indent}                    ReturnType = \"{method.ReturnType}\",");
                source.AppendLine($"{indent}                    Modifiers = new List<string> {{ {string.Join(", ", method.Modifiers.Select(m => $"\"{m.Text}\""))} }},");
                source.AppendLine($"{indent}                    Parameters = new List<{astNamespace}.ASTParameter>");
                source.AppendLine($"{indent}                    {{");
                
                foreach (var parameter in method.ParameterList.Parameters)
                {
                    source.AppendLine($"{indent}                        new {astNamespace}.ASTParameter");
                    source.AppendLine($"{indent}                        {{");
                    source.AppendLine($"{indent}                            Kind = \"Parameter\",");
                    source.AppendLine($"{indent}                            Name = \"{parameter.Identifier.Text}\",");
                    source.AppendLine($"{indent}                            Type = \"{parameter.Type}\"");
                    source.AppendLine($"{indent}                        }},");
                }
                
                source.AppendLine($"{indent}                    }},");
                
                // Add method body
                if (method.Body != null)
                {
                    // Process body statements
                    GenerateStatementsAST(method.Body, source, indent + "                    ", astNamespace);
                }
                else if (method.ExpressionBody != null)
                {
                    // Expression-bodied method
                    string expressionText = method.ExpressionBody.ToString().Replace("\"", "\"\"");
                    source.AppendLine($"{indent}                    Body = new List<{astNamespace}.ASTStatement>");
                    source.AppendLine($"{indent}                    {{");
                    source.AppendLine($"{indent}                        new {astNamespace}.ASTStatement");
                    source.AppendLine($"{indent}                        {{");
                    source.AppendLine($"{indent}                            Kind = \"ExpressionBody\",");
                    source.AppendLine($"{indent}                            Text = @\"{expressionText}\",");
                    source.AppendLine($"{indent}                            Expressions = new List<{astNamespace}.ASTExpression>");
                    source.AppendLine($"{indent}                            {{");
                    source.AppendLine($"{indent}                                new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}                                {{");
                    source.AppendLine($"{indent}                                    Kind = \"ExpressionBodyExpression\",");
                    source.AppendLine($"{indent}                                    Text = @\"{expressionText}\"");
                    source.AppendLine($"{indent}                                }}");
                    source.AppendLine($"{indent}                            }}");
                    source.AppendLine($"{indent}                        }}");
                    source.AppendLine($"{indent}                    }}");
                }
                else
                {
                    source.AppendLine($"{indent}                    Body = new List<{astNamespace}.ASTStatement>()");
                }
                
                source.AppendLine($"{indent}                }},");
            }
            
            source.AppendLine($"{indent}            }}");
            source.AppendLine($"{indent}        }};");
            source.AppendLine();
            source.AppendLine($"{indent}        return ast;");
            source.AppendLine($"{indent}    }}");
            
            source.AppendLine($"{indent}}}");
            
            if (!isGlobalNamespace)
            {
                source.AppendLine("}");
            }
            
            return source.ToString();
        }
        
        /// <summary>
        /// Generate AST for statements
        /// </summary>
        private void GenerateStatementsAST(BlockSyntax block, StringBuilder source, string indent, string astNamespace)
        {
            source.AppendLine($"{indent}Body = new List<{astNamespace}.ASTStatement>");
            source.AppendLine($"{indent}{{");
            
            // Build AST for each statement
            foreach (var statement in block.Statements)
            {
                GenerateStatementNode(statement, source, indent + "    ", astNamespace);
            }
            
            source.AppendLine($"{indent}}},");
        }
        
        /// <summary>
        /// Generate AST for a single statement node
        /// </summary>
        private void GenerateStatementNode(StatementSyntax statement, StringBuilder source, string indent, string astNamespace)
        {
            // Handle different statement types differently to build a more complete AST
            if (statement is BlockSyntax blockStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTBlockStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"Block\",");
                source.AppendLine($"{indent}    Text = @\"{statement.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    Statements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var childStatement in blockStmt.Statements)
                {
                    GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is IfStatementSyntax ifStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"IfStatement\",");
                source.AppendLine($"{indent}    Text = @\"{ifStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract expressions
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"Condition\",");
                source.AppendLine($"{indent}            Text = @\"{ifStmt.Condition.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}            ExpressionType = \"Condition\"");
                source.AppendLine($"{indent}        }}");
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (then clause)
                source.AppendLine($"{indent}    ChildStatements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                // Process the main if body
                source.AppendLine($"{indent}        new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"ThenClause\",");
                source.AppendLine($"{indent}            Text = @\"Then\"");
                source.AppendLine($"{indent}        }},");
                
                if (ifStmt.Statement is BlockSyntax thenBlock)
                {
                    foreach (var childStatement in thenBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                    }
                }
                else
                {
                    GenerateStatementNode(ifStmt.Statement, source, indent + "        ", astNamespace);
                }
                
                // Process the else clause if it exists
                if (ifStmt.Else != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTStatement");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ElseClause\",");
                    source.AppendLine($"{indent}            Text = @\"Else\"");
                    source.AppendLine($"{indent}        }},");
                    
                    if (ifStmt.Else.Statement is BlockSyntax elseBlock)
                    {
                        foreach (var childStatement in elseBlock.Statements)
                        {
                            GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                        }
                    }
                    else
                    {
                        GenerateStatementNode(ifStmt.Else.Statement, source, indent + "        ", astNamespace);
                    }
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ForStatementSyntax forStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ForStatement\",");
                source.AppendLine($"{indent}    Text = @\"{forStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract initializer, condition, incrementor
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Initializer
                if (forStmt.Declaration != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ForInitializer\",");
                    source.AppendLine($"{indent}            Text = @\"{forStmt.Declaration.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }},");
                }
                
                // Condition
                if (forStmt.Condition != null)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ForCondition\",");
                    source.AppendLine($"{indent}            Text = @\"{forStmt.Condition.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }},");
                }
                
                // Incrementors
                foreach (var incrementor in forStmt.Incrementors)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"ForIncrementor\",");
                    source.AppendLine($"{indent}            Text = @\"{incrementor.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }},");
                }
                
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (for body)
                source.AppendLine($"{indent}    ChildStatements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                if (forStmt.Statement is BlockSyntax forBlock)
                {
                    foreach (var childStatement in forBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                    }
                }
                else
                {
                    GenerateStatementNode(forStmt.Statement, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is WhileStatementSyntax whileStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"WhileStatement\",");
                source.AppendLine($"{indent}    Text = @\"{whileStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract condition
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"Condition\",");
                source.AppendLine($"{indent}            Text = @\"{whileStmt.Condition.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}        }}");
                source.AppendLine($"{indent}    }},");
                
                // Add child statements (while body)
                source.AppendLine($"{indent}    ChildStatements = new List<{astNamespace}.ASTStatement>");
                source.AppendLine($"{indent}    {{");
                
                if (whileStmt.Statement is BlockSyntax whileBlock)
                {
                    foreach (var childStatement in whileBlock.Statements)
                    {
                        GenerateStatementNode(childStatement, source, indent + "        ", astNamespace);
                    }
                }
                else
                {
                    GenerateStatementNode(whileStmt.Statement, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ExpressionStatementSyntax exprStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ExpressionStatement\",");
                source.AppendLine($"{indent}    Text = @\"{exprStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Extract the expression
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                GenerateExpressionNode(exprStmt.Expression, source, indent + "        ", astNamespace);
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (statement is ReturnStatementSyntax returnStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ReturnStatement\",");
                source.AppendLine($"{indent}    Text = @\"{returnStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // Add the return expression if there is one
                if (returnStmt.Expression != null)
                {
                    source.AppendLine($"{indent}    ,");
                    source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                    source.AppendLine($"{indent}    {{");
                    
                    GenerateExpressionNode(returnStmt.Expression, source, indent + "        ", astNamespace);
                    
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}},");
            }
            else if (statement is LocalDeclarationStatementSyntax declStmt)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"LocalDeclaration\",");
                source.AppendLine($"{indent}    Text = @\"{declStmt.ToString().Replace("\"", "\"\"")}\"");
                
                // For each variable declarator, add an expression
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    Expressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var variable in declStmt.Declaration.Variables)
                {
                    source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            Kind = \"VariableDeclarator\",");
                    source.AppendLine($"{indent}            Text = @\"{variable.ToString().Replace("\"", "\"\"")}\"");
                    
                    // If there's an initializer, include it
                    if (variable.Initializer != null)
                    {
                        source.AppendLine($"{indent}            ,");
                        source.AppendLine($"{indent}            ChildExpressions = new List<{astNamespace}.ASTExpression>");
                        source.AppendLine($"{indent}            {{");
                        
                        source.AppendLine($"{indent}                new {astNamespace}.ASTExpression");
                        source.AppendLine($"{indent}                {{");
                        source.AppendLine($"{indent}                    Kind = \"Initializer\",");
                        source.AppendLine($"{indent}                    Text = @\"{variable.Initializer.Value.ToString().Replace("\"", "\"\"")}\"");
                        source.AppendLine($"{indent}                }}");
                        
                        source.AppendLine($"{indent}            }}");
                    }
                    
                    source.AppendLine($"{indent}        }},");
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else
            {
                // Default handling for other statement types
                source.AppendLine($"{indent}new {astNamespace}.ASTStatement");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"{statement.Kind()}\",");
                source.AppendLine($"{indent}    Text = @\"{statement.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}}},");
            }
        }
        
        /// <summary>
        /// Generate AST for an expression node
        /// </summary>
        private void GenerateExpressionNode(ExpressionSyntax expression, StringBuilder source, string indent, string astNamespace)
        {
            if (expression is BinaryExpressionSyntax binaryExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"BinaryExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = \"{binaryExpr.OperatorToken.Text}\",");
                source.AppendLine($"{indent}    ChildExpressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Left expression
                GenerateExpressionNode(binaryExpr.Left, source, indent + "        ", astNamespace);
                
                // Right expression
                GenerateExpressionNode(binaryExpr.Right, source, indent + "        ", astNamespace);
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is InvocationExpressionSyntax invocationExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTInvocationExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"InvocationExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = \"MethodCall\",");
                
                // Generate method info with target
                string methodName = invocationExpr.Expression is MemberAccessExpressionSyntax maExpr 
                    ? maExpr.Name.ToString()
                    : invocationExpr.Expression.ToString();
                
                source.AppendLine($"{indent}    MethodInfo = new {astNamespace}.ASTMethodInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        Name = @\"{methodName.Replace("\"", "\"\"")}\"");
                
                // If it's a member access, we can determine the declaring type
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    source.AppendLine($"{indent}        ,");
                    source.AppendLine($"{indent}        DeclaringType = new {astNamespace}.ASTTypeInfo");
                    source.AppendLine($"{indent}        {{");
                    source.AppendLine($"{indent}            TypeName = @\"{memberAccess.Expression.ToString().Replace("\"", "\"\"")}\"");
                    source.AppendLine($"{indent}        }}");
                }
                
                source.AppendLine($"{indent}    }},");
                
                // Target expression (e.g., the object the method is called on)
                source.AppendLine($"{indent}    TargetExpression = new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        Kind = \"Target\",");
                source.AppendLine($"{indent}        Text = @\"{invocationExpr.Expression.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}    }},");
                
                // Arguments
                source.AppendLine($"{indent}    Arguments = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                foreach (var arg in invocationExpr.ArgumentList.Arguments)
                {
                    // For each argument, recursively generate its expression
                    GenerateExpressionNode(arg.Expression, source, indent + "        ", astNamespace);
                }
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is AssignmentExpressionSyntax assignmentExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"AssignmentExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = \"{assignmentExpr.OperatorToken.Text}\",");
                source.AppendLine($"{indent}    ChildExpressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Left expression (target of assignment)
                source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"Left\",");
                source.AppendLine($"{indent}            Text = @\"{assignmentExpr.Left.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}        }},");
                
                // Right expression (value being assigned) - recursive parsing for complex expressions
                GenerateExpressionNode(assignmentExpr.Right, source, indent + "        ", astNamespace);
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"MemberAccessExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                source.AppendLine($"{indent}    ExpressionType = \"{memberAccessExpr.OperatorToken.Text}\",");
                source.AppendLine($"{indent}    ChildExpressions = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                // Expression being accessed - recursive for nested member access expressions
                GenerateExpressionNode(memberAccessExpr.Expression, source, indent + "        ", astNamespace);
                
                // Name of member being accessed
                source.AppendLine($"{indent}        new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}        {{");
                source.AppendLine($"{indent}            Kind = \"Name\",");
                source.AppendLine($"{indent}            Text = @\"{memberAccessExpr.Name.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}        }}");
                
                source.AppendLine($"{indent}    }}");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is ObjectCreationExpressionSyntax objectCreationExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTObjectCreationExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"ObjectCreationExpression\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\",");
                
                // Detailed type information
                source.AppendLine($"{indent}    CreatedType = new {astNamespace}.ASTTypeInfo");
                source.AppendLine($"{indent}    {{");
                source.AppendLine($"{indent}        TypeName = @\"{objectCreationExpr.Type.ToString().Replace("\"", "\"\"")}\",");
                
                // Parse the namespace from the type if possible
                string typeName = objectCreationExpr.Type.ToString();
                int lastDot = typeName.LastIndexOf('.');
                if (lastDot > 0)
                {
                    string ns = typeName.Substring(0, lastDot);
                    source.AppendLine($"{indent}        Namespace = @\"{ns.Replace("\"", "\"\"")}\",");
                }
                
                // Check if it's a generic type
                if (objectCreationExpr.Type is GenericNameSyntax genericType)
                {
                    source.AppendLine($"{indent}        IsGeneric = true,");
                    source.AppendLine($"{indent}        TypeArguments = new List<{astNamespace}.ASTTypeInfo>");
                    source.AppendLine($"{indent}        {{");
                    
                    foreach (var typeArg in genericType.TypeArgumentList.Arguments)
                    {
                        source.AppendLine($"{indent}            new {astNamespace}.ASTTypeInfo");
                        source.AppendLine($"{indent}            {{");
                        source.AppendLine($"{indent}                TypeName = @\"{typeArg.ToString().Replace("\"", "\"\"")}\"");
                        source.AppendLine($"{indent}            }},");
                    }
                    
                    source.AppendLine($"{indent}        }}");
                }
                
                source.AppendLine($"{indent}    }},");
                
                // Constructor arguments with proper recursive expression parsing
                source.AppendLine($"{indent}    Arguments = new List<{astNamespace}.ASTExpression>");
                source.AppendLine($"{indent}    {{");
                
                if (objectCreationExpr.ArgumentList != null)
                {
                    foreach (var arg in objectCreationExpr.ArgumentList.Arguments)
                    {
                        GenerateExpressionNode(arg.Expression, source, indent + "        ", astNamespace);
                    }
                }
                
                source.AppendLine($"{indent}    }}");
                
                // Handle initializers if present
                if (objectCreationExpr.Initializer != null)
                {
                    source.AppendLine($"{indent}    ,");
                    source.AppendLine($"{indent}    Initializers = new List<{astNamespace}.ASTExpression>");
                    source.AppendLine($"{indent}    {{");
                    
                    foreach (var initializer in objectCreationExpr.Initializer.Expressions)
                    {
                        GenerateExpressionNode(initializer, source, indent + "        ", astNamespace);
                    }
                    
                    source.AppendLine($"{indent}    }}");
                }
                
                source.AppendLine($"{indent}}},");
            }
            else if (expression is LiteralExpressionSyntax literalExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"Literal\",");
                source.AppendLine($"{indent}    Text = @\"{literalExpr.Token.Text.Replace("\"", "\"\"")}\"");
                
                // Include literal type information
                source.AppendLine($"{indent}    ,");
                source.AppendLine($"{indent}    ExpressionType = \"{literalExpr.Token.ValueText}\"");
                
                source.AppendLine($"{indent}}},");
            }
            else if (expression is IdentifierNameSyntax identifierExpr)
            {
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"Identifier\",");
                source.AppendLine($"{indent}    Text = @\"{identifierExpr.Identifier.Text.Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}}},");
            }
            else if (expression is ParenthesizedExpressionSyntax parenthesizedExpr)
            {
                // For parenthesized expressions, process the expression inside
                GenerateExpressionNode(parenthesizedExpr.Expression, source, indent, astNamespace);
            }
            else
            {
                // Default handling for other expression types
                source.AppendLine($"{indent}new {astNamespace}.ASTExpression");
                source.AppendLine($"{indent}{{");
                source.AppendLine($"{indent}    Kind = \"{expression.Kind()}\",");
                source.AppendLine($"{indent}    Text = @\"{expression.ToString().Replace("\"", "\"\"")}\"");
                source.AppendLine($"{indent}}},");
            }
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                    classDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateClasses.Add(classDeclarationSyntax);
                }
            }
        }
    }
}
