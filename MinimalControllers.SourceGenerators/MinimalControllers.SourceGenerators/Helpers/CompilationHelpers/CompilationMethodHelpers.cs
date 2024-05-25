using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MinimalControllers.SourceGenerators.Helpers.CompilationHelpers;

public static class CompilationMethodHelpers
{
    public static IEnumerable<ArgumentParameter> GetMethodArguments(IMethodSymbol method)
        => method
            .Parameters
            .Select(x => new ArgumentParameter(
                x.Name, 
                x.Type.ToString(), 
                GetMethodAttribute(x)));

    private static List<AttributeParameter>? GetMethodAttribute(IParameterSymbol parameterSyntax)
    {
        var result = parameterSyntax
            .GetAttributes()
            .OfType<AttributeData>()
            .ToList();
        
        var attributeParameters = result.Select(x => 
                new AttributeParameter(x.AttributeClass.ToString(),
            x.NamedArguments.ToDictionary(
                y => y.Key,
                y =>
                {
                    if (y.Value.Type.ToString().ToLower() == "string")
                    {
                        return $"\"{y.Value.Value}\"";
                    }
                    
                    return $"({y.Value.Type}){y.Value.Value}";
                    // if (y.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    // {
                    //     var result = NamespaceHelper.GetNamespace(compilation, memberAccessExpressionSyntax);
                    //     return $"{result}.{y.Expression.ToString().Replace($"{result}.", "")}";
                    // }
                    //
                    // return y.Expression.ToString();
                })))
            .ToList();
        
        return attributeParameters;

        return null;
    }
    
    public static string GetMethodEndpoint(IMethodSymbol methodDeclarationSyntax, string httpMethod)
    {
        var attribute = methodDeclarationSyntax.GetAttributes()
            .Where(x => x.AttributeClass.Name.ToLower().Contains("http"))
            .FirstOrDefault(x => AttributeHelpers
                .AttributeEndsWithAny(
                    x.ToString(),
                    [httpMethod]));
        
        if (!attribute.ConstructorArguments.Any())
        {
            return $"/{methodDeclarationSyntax.Name}";
        }
        
        return attribute.ConstructorArguments.First().Value.ToString();

        // if(attribute?.ArgumentList?.Arguments
        //        .FirstOrDefault()?.Expression 
        //    is not LiteralExpressionSyntax literalExpressionSyntax)
        //     return $"/{methodDeclarationSyntax.Identifier.Text}";
        //
        // var endpoint = literalExpressionSyntax.Token.ValueText;
        //
        // if (!endpoint.StartsWith("/"))
        //     endpoint = $"/{endpoint}";

        // return endpoint;
    }
}