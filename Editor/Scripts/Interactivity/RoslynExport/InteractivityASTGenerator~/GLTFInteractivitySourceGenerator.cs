using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityGLTF.Interactivity.AST;
using InteractivityASTGenerator.Generators;

namespace InteractivityASTGenerator
{
    /// <summary>
    /// Source generator that creates AST classes for GLTFInteractivity-attributed classes
    /// </summary>
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
                    
                    // Generate the GetAST method - pass the semantic model
                    string classSource = GetASTMethodGenerator.GenerateGetASTMethod(classDeclaration, classSymbol, model);
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
        /// Syntax receiver to identify candidate classes
        /// </summary>
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