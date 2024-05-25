using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MinimalControllers.SourceGenerators.Helpers;

public static class NamespaceHelper
{
    public static string GetNamespace(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

        var symbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax).ContainingNamespace;
        return symbol.ToDisplayString();
    }
    
    public static string GetNamespace(Compilation compilation, MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(memberAccessExpressionSyntax.SyntaxTree);

        var symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol.ContainingNamespace;
        return symbol.ToDisplayString();
    }
    
    public static string GetNamespace(SemanticModel semanticModel, AttributeSyntax classDeclarationSyntax)
    {
        try
        {
            var symbol = semanticModel.GetSymbolInfo(classDeclarationSyntax).Symbol;

            return $"{symbol.ContainingNamespace}.{classDeclarationSyntax.Name.ToString().Split('.').Last()}";
        }
        catch
        {
            return string.Empty;
        }
    }
    
    public static string GetNamespace(Compilation compilation, AttributeSyntax classDeclarationSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
        
        return GetNamespace(semanticModel, classDeclarationSyntax);
    }
    
    public static string GetNamespace(Compilation compilation, ParameterSyntax parameterSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(parameterSyntax.Type.SyntaxTree);

        var symbol = semanticModel.GetSymbolInfo(parameterSyntax.Type).Symbol.ToString();

        return symbol;
    }
}