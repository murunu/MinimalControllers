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
            .Any(attributeSyntax => HttpAttributeDefinitions
                .ControllerTypesWithNamespace
                .Any(name =>
                    name
                        .Equals(NamespaceHelper.GetNamespace(context.SemanticModel, attributeSyntax))))
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
                var methodArguments = GetMethodArguments(method.Key).ToList();
                foreach (var httpMethod in method.Value)
                {
                    source.AddEndpoint(
                        httpMethod, 
                        $"/{method.Key.Identifier.Text}", 
                        method.Key.Identifier.Text,
                        controllerServices,
                        methodArguments);
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
    
    private static string GetControllerRoute(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        return GetArgumentValue(compilation, classDeclarationSyntax, HttpAttributeDefinitions.RouteWithNamespace, 0);
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
                HttpMethods = GetAttributesByList(compilation, method, HttpAttributeDefinitions.HttpMethodsWithNamespace)
            })
            .Where(x => x.HttpMethods.Any())
            .ToDictionary(
                x => x.Method,
                x => x.HttpMethods
                    .Select(y => y.Name
                        .ToString().Split('.').Last().Replace("Http", ""))
                    .ToArray());
    
    private static IEnumerable<string> GetMethodArguments(BaseMethodDeclarationSyntax method)
        => method
            .ParameterList
            .Parameters
            .Select(parameter => parameter.Type.ToString());

    private static IEnumerable<AttributeSyntax> GetAttributesByList(Compilation compilation, MemberDeclarationSyntax classDeclarationSyntax,
        IEnumerable<string> names)
        => classDeclarationSyntax
            .AttributeLists
            .SelectMany(attributeList => attributeList.Attributes)
            .Where(attribute =>
                names.Any(
                    name =>
                        name.Equals(
                            NamespaceHelper.GetNamespace(compilation, attribute))));
}