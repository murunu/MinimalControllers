using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MinimalControllers.SourceGenerators.Helpers;
using MinimalControllers.SourceGenerators.Helpers.CompilationHelpers;

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
        
        // Add the MinimalConverter attribute to the compilation
        HttpAttributeDefinitions.AddMinimalConverterAttributeToCompilation(context);
        
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

        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (symbol.IsAbstract)
        {
            return (classDeclarationSyntax, false);
        }

        return (classDeclarationSyntax, GetApiControllerAttribute(symbol));
        // Go through all attributes of the class.
        // return classDeclarationSyntax
        //     .AttributeLists
        //     .SelectMany(attributeListSyntax => attributeListSyntax.Attributes)
        //     .Any(syntax => AttributeHelpers.AttributeEndsWithAny(
        //         syntax.ToString(), 
        //         HttpAttributeDefinitions.ControllerTypes))
        //     ? (classDeclarationSyntax, true)
        //     : (classDeclarationSyntax, false);
    }

    private static bool GetApiControllerAttribute(INamedTypeSymbol symbol)
    {
        if (symbol.GetAttributes().Any(x =>
                AttributeHelpers.AttributeEndsWithAny(
                    x.ToString(),
                    HttpAttributeDefinitions.ControllerTypes)))
        {
            return true;
        }

        if (symbol.BaseType == null)
        {
            return false;
        }

        return GetApiControllerAttribute(symbol.BaseType);
    }
    private static void GenerateCode(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        var source = new MinimalApiClassBuilder();
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var controllerName = GetControllerName(compilation, classDeclarationSyntax);

            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            
            if(symbol == null)
                continue;

            if (string.IsNullOrEmpty(controllerName))
                continue;
            
            
            var controllerRoute = GetControllerRoute(classDeclarationSyntax);
            
            var controllerServices = CompilationNamedTypeHelpers.GetControllerServices(symbol).ToList();

            var methods = GetHttpMethods(symbol);
            
            if(methods == null)
                continue;

            source.AddGroup(controllerName, controllerRoute);

            foreach (var method in methods)
            {
                var methodArguments = CompilationMethodHelpers.GetMethodArguments(method.Key).ToList();

                foreach (var httpMethod in method.Value)
                {
                    var endpoint = CompilationMethodHelpers.GetMethodEndpoint(method.Key, httpMethod);
                    if (endpoint.Any(x => x.Equals('{')))
                    {
                        var variableName = endpoint
                            .Split('{')
                            .Last()
                            .Split('}')
                            .First()
                            .Split(':')
                            .First();

                        if (methodArguments.Any(x => x.Name.Equals(variableName)))
                        {
                            var index = methodArguments.FindIndex(x => x.Name.Equals(variableName));
                            
                            if(!methodArguments[index].Attribute.Any(x => x.Name.ToLower().Contains("fromroute")))
                            {
                                methodArguments[index].Attribute.Add(
                                    new AttributeParameter(
                                        "Microsoft.AspNetCore.Mvc.FromRouteAttribute",
                                        new Dictionary<string, string>
                                        {
                                            {"Name", $"\"{methodArguments[index].Name}\"" }
                                        }));
                            }
                        }
                    }

                    if (methodArguments.Any(x => !x.Attribute.Any()))
                    {
                        var index = methodArguments.FindIndex(x => !x.Attribute.Any());
                        
                        methodArguments[index].Attribute.Add(new AttributeParameter(
                            "Microsoft.AspNetCore.Mvc.FromBodyAttribute"
                            ));
                    }

                    source.AddEndpoint(
                        httpMethod,
                        CompilationMethodHelpers.GetMethodEndpoint(method.Key, httpMethod),
                        method.Key.Name,
                        controllerServices,
                        methodArguments,
                        method.Key.IsAsync);
                }
            }
        }

        // Add the source code to the compilation.
        context.AddSource("UseControllers.g.cs", SourceText.From(source.Build(), Encoding.UTF8));
    }

    private static string GetControllerName(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        return $"{NamespaceHelper.GetNamespace(compilation, classDeclarationSyntax)}.{classDeclarationSyntax.Identifier.Text}";
    }
    
    private static string GetControllerRoute(ClassDeclarationSyntax classDeclarationSyntax)
    {
        return GetArgumentValue(classDeclarationSyntax, HttpAttributeDefinitions.Route, 0);
    }

    private static string GetArgumentValue(ClassDeclarationSyntax classDeclarationSyntax, string attributeName, int argumentIndex)
    {
        foreach (var attribute in GetAttributesByList(classDeclarationSyntax, [attributeName]))
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

    private static IEnumerable<IMethodSymbol>? GetMembers(INamedTypeSymbol? symbol, int depth = 0, int maxDepth = 10)
    {
        if (depth > maxDepth)
        {
            return null;
        }
        
        if (symbol == null)
        {
            return null;
        }

        var result = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => !x.IsOverride)
            .ToList();

        var childMembers = GetMembers(symbol.BaseType, depth + 1);

        if (childMembers != null)
        {
            result.AddRange(childMembers);
        }
        
        return result;
    }
    
    private static Dictionary<IMethodSymbol, string[]>? GetHttpMethods(INamedTypeSymbol symbol)
    {
        return GetMembers(symbol)?
            .Select(method => new
            {
                Method = method,
                HttpMethods = method.GetAttributes()
                    .Where(x => 
                        AttributeHelpers
                            .AttributeEndsWithAny(
                                x.ToString(), 
                                HttpAttributeDefinitions.HttpMethods))
            })
            .Where(x => x.HttpMethods.Any())
            .ToDictionary(
                x => x.Method,
                x => x.HttpMethods
                    .Select(y => y
                        .ToString()
                        .Split('.')
                        .Last()
                        .Replace("Http", "")
                        .Replace("Attribute", "")
                        .Split('(')
                        .First())
                    .ToArray());
        
        // return classDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>()
        //     .Select(method => new
        //     {
        //         Method = method,
        //         HttpMethods = GetAttributesByList(compilation, method, HttpAttributeDefinitions.HttpMethods)
        //     })
        //     .Where(x => x.HttpMethods.Any())
        //     .ToDictionary(
        //         x => x.Method,
        //         x => x.HttpMethods
        //             .Select(y => y.Name
        //                 .ToString().Split('.').Last().Replace("Http", ""))
        //             .ToArray());
    }

    private static IEnumerable<AttributeSyntax> GetAttributesByList(MemberDeclarationSyntax classDeclarationSyntax,
        IEnumerable<string> names)
        => classDeclarationSyntax
            .AttributeLists
            .SelectMany(attributeList => attributeList.Attributes)
            .Where(attribute =>
                AttributeHelpers.AttributeEndsWithAny(attribute.ToString(), names));
}