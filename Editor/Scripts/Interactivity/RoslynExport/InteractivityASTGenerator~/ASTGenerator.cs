using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InteractivityASTGenerator.Generators;

// This file has been split into multiple files for better maintainability.
// See:
// - Models/ASTNode.cs - Base AST node definition
// - Models/ASTClass.cs - Class definition model
// - Models/ASTMembers.cs - Models for fields, properties, methods, parameters
// - Models/ASTStatements.cs - Statement models
// - Models/ASTExpressions.cs - Expression models
// - Models/ASTTypeInfo.cs - Type information models
// - Generators/ASTClassGenerator.cs - AST class generation
// - Generators/GetASTMethodGenerator.cs - GetAST method generation
// - Generators/ASTStatementGenerator.cs - Statement/expression generation
// - GLTFInteractivitySourceGenerator.cs - Main source generator implementation
