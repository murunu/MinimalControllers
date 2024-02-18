﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MinimalControllers.SourceGenerators.Helpers;

public class MinimalApiClassBuilder
{
    private const string ClassName = "MinimalController";
    private string _currentController = "";
    private string _currentControllerName = "";
    private bool _currentControllerHasAction = false;
    
    private string GetGroupVariableName() => _currentController.Split('.').Last().ToLower() + "Group";
    
    private readonly StringBuilder _builder = new($$"""
                                                    // <auto-generated/>
                                                    
                                                    namespace {{HttpAttributeDefinitions.Namespace}} {
                                                        public static class {{ClassName}} {
                                                            public static WebApplication UseControllers(this WebApplication app)
                                                            {
                                                    """);

    public MinimalApiClassBuilder AddGroup(string name, string endpoint)
    {
        _currentController = name;
        
        _currentControllerName = name.Split('.').Last().ToLower().Replace("controller", "");

        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = _currentController;
        }

        endpoint = endpoint.ToLower().Replace("[controller]", _currentControllerName);

        if (!endpoint.StartsWith("/"))
            endpoint = $"/{endpoint}";
        
        _builder.Append($"\n\n\t\t\tvar {GetGroupVariableName()} = app.MapGroup(\"{endpoint}\");");
        return this;
    }

    public MinimalApiClassBuilder AddEndpoint(
        string httpMethod, 
        string endpoint, 
        string methodName,
        List<string> controllerServices,
        List<string> methodArguments)
    {
        
        if(string.IsNullOrEmpty(_currentController))
            throw new InvalidOperationException("You must call AddGroup before adding an endpoint.");

        var services = controllerServices.ToDictionary(
            x => x,
            x => RandomString(x)
            );

        var methodArgumentDictionary = methodArguments
            .ToDictionary(
                x => x,
                x => RandomString(x)
                );

        var combinedServices = services
            .Select(x => $"[Microsoft.AspNetCore.Mvc.FromServicesAttribute]{x.Key} {x.Value}")
            .Concat(methodArgumentDictionary
                .Select(x => $"{x.Key} {x.Value}"));

        _builder.Append($$"""
                                         
                                         
                                      {{
                                          GetGroupVariableName()
                                      }}.Map{{httpMethod}}("{{endpoint}}", ({{
                                          string.Join(",\n\t\t\t\t", combinedServices)
                                      }}) => {
                                          var controller = new {{
                                              _currentController
                                          }}({{
                                              string.Join(",\n\t\t\t\t", services.Values)
                                          }});
                                        
                                          var result = controller.{{methodName}}({{
                                              string.Join(",\n\t\t\t\t", methodArgumentDictionary.Values)
                                          }});
                                          
                                          if(MinimalControllers.Converters.TryConvertToIResult(result, out var iResult))
                                              return iResult;
                                          
                                          return Microsoft.AspNetCore.Http.Results.Ok(result);
                                      });
                          """);
        return this;
    }
    
    public string Build() =>
        _builder.AppendLine("\n\t\t\treturn app;\n\t\t}\n\t}\n}").ToString();

    private static string RandomString(string input, int length = 32)
    {
        var random = new Random(Seed: input.GetHashCode());
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        return $"{NormalizeName(input)}_{new string(
            Enumerable
                .Repeat(chars, length)
                .Select(s => s[random
                    .Next(s.Length)])
                .ToArray())}";
    }
    
    private static string NormalizeName(string name) =>
        name
            .Split('.')
            .Last()
            .Split('<')
            .First()
            .ToLower(CultureInfo.InvariantCulture);
}