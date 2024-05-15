﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MinimalControllers.SourceGenerators.Helpers;

public static class HttpAttributeDefinitions
{
    public const string Namespace = "MinimalControllers";

    public static readonly string[] HttpMethods = [
        "HttpGet", 
        "HttpPost", 
        "HttpPut", 
        "HttpPatch", 
        "HttpDelete"];
    
    public static readonly string[] ControllerTypes = [
        "Controller", 
        "ApiController"
    ];
    
    public const string Route = "Route";


    public static readonly string[] HttpMethodsWithNamespace = HttpMethods.Select(x => $"{Namespace}.{x}").ToArray();
    public static readonly string[] ControllerTypesWithNamespace = ControllerTypes.Select(x => $"{Namespace}.{x}").ToArray();
    public const string RouteWithNamespace = $"{Namespace}.{Route}";
    
    private static Dictionary<string, string> GetAttributes(
        IEnumerable<string> attributeNames, 
        IEnumerable<string> ctorArguments,
        params AttributeTargets[] attributeTargets) =>
        attributeNames
            .ToDictionary(
                name => name,
                name => $$"""
                                // <auto-generated/>

                                namespace {{Namespace}}
                                {
                                    [System.AttributeUsage({{
                                        string.Join(" | ", attributeTargets.Select(x => $"System.AttributeTargets.{x}"))
                                    }}, Inherited = false, AllowMultiple = false)]
                                    public sealed class {{name}}Attribute : System.Attribute
                                    {
                                        public {{name}}Attribute({{
                                            string.Join(", ", ctorArguments)
                                        }})
                                        {
                                        }
                                    }
                                }
                                """);
    
    private static void AddAttributesFromNames(
        IEnumerable<string> attributeNames, 
        IEnumerable<string> ctorArguments,
        IncrementalGeneratorInitializationContext context, 
        params AttributeTargets[] attributeTargets)
    {
        foreach (var item in GetAttributes(attributeNames, ctorArguments, attributeTargets))
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                $"{item.Key}.g.cs",
                SourceText.From(item.Value, Encoding.UTF8)));
        }
    }
    
    public static void AddHttpAttributesToCompilation(IncrementalGeneratorInitializationContext context)
        => AddAttributesFromNames(HttpMethods, ["string name = \"\""], context, AttributeTargets.Method);

    public static void AddApiControllerAttributesToCompilation(IncrementalGeneratorInitializationContext context)
        => AddAttributesFromNames(ControllerTypes, [], context, AttributeTargets.Class);

    public static void AddRouteAttributesToCompilation(IncrementalGeneratorInitializationContext context)
        => AddAttributesFromNames([Route], ["string name"], context, AttributeTargets.Class);
}