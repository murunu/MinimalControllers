using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MinimalControllers.SourceGenerators.Helpers;

namespace MinimalControllers.SourceGenerators;

[Generator]
public class MinimalControllersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the default http attributes to the compilation.
        HttpAttributeDefinitions.AddHttpAttributesToCompilation(context);

        // Add the ApiController attribute to the compilation.
        HttpAttributeDefinitions.AddApiControllerAttributesToCompilation(context);
        
        // Add the Route attribute to the compilation
        HttpAttributeDefinitions.AddRouteAttributesToCompilation(context);
        
        // Add the extensions to the compilation
        Extensions.AddConvertToIResultMethod(context);

        // Filter classes annotated with the [ApiController] attribute. Only filtered Syntax Nodes can trigger code generation.
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.apiControllerAttributeFound)
            .Select((t, _) => t.classDeclarationSyntax);

        // Generate the source code.
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    private static (
        ClassDeclarationSyntax classDeclarationSyntax,
        bool apiControllerAttributeFound
        ) GetClassDeclarationForSourceGen(
            GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // Go through all attributes of the class.
        return classDeclarationSyntax
            .AttributeLists
            .SelectMany(attributeListSyntax => attributeListSyntax.Attributes)
            .Any(syntax => AttributeEndsWithAny(syntax.ToString(), HttpAttributeDefinitions.ControllerTypes))
            ? (classDeclarationSyntax, true)
            : (classDeclarationSyntax, false);
    }

    private static void GenerateCode(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        var source = new MinimalApiClassBuilder();
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var controllerName = GetControllerName(compilation, classDeclarationSyntax);
            var controllerRoute = GetControllerRoute(compilation, classDeclarationSyntax);
            
            var controllerServices = GetControllerServices(compilation, classDeclarationSyntax).ToList();
            
            if (string.IsNullOrEmpty(controllerName))
                continue;

            var methods = GetHttpMethods(compilation, classDeclarationSyntax);

            source.AddGroup(controllerName, controllerRoute);

            foreach (var method in methods)
            {
                var methodArguments = GetMethodArguments(compilation, method.Key).ToList();
                foreach (var httpMethod in method.Value)
                {
                    source.AddEndpoint(
                        httpMethod, 
                        GetMethodEndpoint(method.Key, httpMethod), 
                        method.Key.Identifier.Text,
                        controllerServices,
                        methodArguments,
                        method.Key.Modifiers.Any(SyntaxKind.AsyncKeyword));
                }
            }
        }

        // Add the source code to the compilation.
        context.AddSource("UseControllers.g.cs", SourceText.From(source.Build(), Encoding.UTF8));
    }

    private static string GetMethodEndpoint(MethodDeclarationSyntax methodDeclarationSyntax, string httpMethod)
    {
        var attribute = methodDeclarationSyntax.AttributeLists.FirstOrDefault(x => AttributeEndsWithAny(x.ToString(), [httpMethod]))?.Attributes.FirstOrDefault();
        
        if(attribute?.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax literalExpressionSyntax)
            return $"/{methodDeclarationSyntax.Identifier.Text}";

        var endpoint = literalExpressionSyntax.Token.ValueText;

        if (!endpoint.StartsWith("/"))
            endpoint = $"/{endpoint}";
        
        return endpoint;
    }

    private static string GetControllerName(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        return $"{NamespaceHelper.GetNamespace(compilation, classDeclarationSyntax)}.{classDeclarationSyntax.Identifier.Text}";
    }
    
    private static string GetControllerRoute(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        return GetArgumentValue(compilation, classDeclarationSyntax, HttpAttributeDefinitions.Route, 0);
    }

    private static IEnumerable<string> GetControllerServices(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
        => classDeclarationSyntax
            .Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .Select(parameter => NamespaceHelper.GetNamespace(compilation, parameter));

    private static string GetArgumentValue(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax, string attributeName, int argumentIndex)
    {
        foreach (var attribute in GetAttributesByList(compilation, classDeclarationSyntax, [attributeName]))
        {
            if (attribute.ArgumentList == null)
                continue;

            if (attribute.ArgumentList.Arguments.Count <= argumentIndex) 
                continue;
            if (attribute.ArgumentList.Arguments[argumentIndex].Expression is LiteralExpressionSyntax literalExpression)
            {
                return literalExpression.Token.ValueText;
            }
        }

        return null;
    }

    private static Dictionary<MethodDeclarationSyntax, string[]> GetHttpMethods(Compilation compilation, TypeDeclarationSyntax classDeclarationSyntax)
        => classDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>()
            .Select(method => new
            {
                Method = method,
                HttpMethods = GetAttributesByList(compilation, method, HttpAttributeDefinitions.HttpMethods)
            })
            .Where(x => x.HttpMethods.Any())
            .ToDictionary(
                x => x.Method,
                x => x.HttpMethods
                    .Select(y => y.Name
                        .ToString().Split('.').Last().Replace("Http", ""))
                    .ToArray());
    
    private static IEnumerable<string> GetMethodArguments(Compilation compilation, BaseMethodDeclarationSyntax method)
        => method
            .ParameterList
            .Parameters
            .Select(x => GetMethodArgumentName(compilation, x));

    private static string GetMethodArgumentName(Compilation compilation, ParameterSyntax parameterSyntax)
    {
        var result = parameterSyntax
            .AttributeLists
            .Aggregate("", (current, item) => current + GetMethodArgumentAttributeName(item.ToString()));

        return $"{result} {NamespaceHelper.GetNamespace(compilation, parameterSyntax)}";
    }
    private static string GetMethodArgumentAttributeName(string attribute)
    {
        attribute = attribute.Replace("[", "").Replace("]", "").Split('.').Last();

        if (!attribute.Any(x => x.Equals('(')))
        {
            return $"[Microsoft.AspNetCore.Mvc.{attribute}Attribute]";
        }

        return $"[Microsoft.AspNetCore.Mvc.{attribute.Split('(').First()}Attribute({attribute.Split('(').Last()}]";
    }

    private static IEnumerable<AttributeSyntax> GetAttributesByList(Compilation compilation, MemberDeclarationSyntax classDeclarationSyntax,
        IEnumerable<string> names)
        => classDeclarationSyntax
            .AttributeLists
            .SelectMany(attributeList => attributeList.Attributes)
            .Where(attribute =>
                AttributeEndsWithAny(attribute.ToString(), names));
    
    private static bool AttributeEndsWithAny(string attribute, IEnumerable<string> names)
    {
        return names.Any(name =>
        {
            var result = attribute.Split('(').FirstOrDefault();
            
            if(string.IsNullOrEmpty(result))
            {
                return false;
            }
            
            return result.EndsWith(name, StringComparison.OrdinalIgnoreCase);
        });
    }
}