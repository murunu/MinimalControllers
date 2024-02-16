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
    
    public static string GetNamespace(SemanticModel semanticModel, AttributeSyntax classDeclarationSyntax)
    {
        try
        {
            var symbol = semanticModel.GetSymbolInfo(classDeclarationSyntax).Symbol;

            return classDeclarationSyntax.Name.ToString().StartsWith(symbol.ContainingNamespace.Name) 
                ? classDeclarationSyntax.Name.ToString() 
                : $"{symbol.ContainingNamespace.Name}.{classDeclarationSyntax.Name}";
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
    
    public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        var nameSpace = string.Empty;

        var potentialNamespaceParent = syntax.Parent;

        while (potentialNamespaceParent != null &&
               potentialNamespaceParent is not NamespaceDeclarationSyntax
               && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        if (potentialNamespaceParent is not BaseNamespaceDeclarationSyntax namespaceParent) 
            return nameSpace;
        
        nameSpace = namespaceParent.Name.ToString();

        while (true)
        {
            if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
            {
                break;
            }

            nameSpace = $"{namespaceParent.Name}.{nameSpace}";
            namespaceParent = parent;
        }

        return nameSpace;
    }
}