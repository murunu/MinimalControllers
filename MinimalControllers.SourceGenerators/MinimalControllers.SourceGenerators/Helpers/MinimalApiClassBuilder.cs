using System;
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
                                                    using Microsoft.AspNetCore.OpenApi;
                                                    using Microsoft.AspNetCore.Builder;
                                                    using Microsoft.AspNetCore.Http;
                                                    
                                                    namespace {{HttpAttributeDefinitions.Namespace}} {
                                                        public static class {{ClassName}} {
                                                            public static WebApplication UseControllers(
                                                                this WebApplication app)
                                                            {
                                                    """);

    public MinimalApiClassBuilder AddGroup(string name, string endpoint)
    {
        _currentController = name;
        
        _currentControllerName = name.Split('.').Last().ToLower().Replace("controller", "");

        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = _currentControllerName;
        }

        endpoint = endpoint.ToLower().Replace("[controller]", _currentControllerName).Replace("[action]", "");

        if (!endpoint.StartsWith("/"))
            endpoint = $"/{endpoint}";

        _builder.Append($"""

                                    var {GetGroupVariableName()} = app.MapGroup("{endpoint}")
                                        .WithTags("{endpoint.Replace("/", "")}")
                                        .WithOpenApi();
                         """);
        return this;
    }

    public MinimalApiClassBuilder AddEndpoint(
        string httpMethod, 
        string endpoint, 
        string methodName,
        List<ArgumentParameter> controllerServices,
        List<ArgumentParameter> methodArguments,
        bool isAsync)
    {
        
        if(string.IsNullOrEmpty(_currentController))
            throw new InvalidOperationException("You must call AddGroup before adding an endpoint.");

        methodArguments = methodArguments
            .Select(x =>
            {
                var item = controllerServices.FirstOrDefault(y => y.TypeName.Equals(x.TypeName));

                if (item != null)
                {
                    x.Name = item.Name;
                }

                return x;
            })
            .ToList();
        
        var combinedServices = controllerServices
            .Where(x =>
                !methodArguments
                    .Any(y =>
                        y.TypeName.Equals(x.TypeName)))
            .Select(x => x.ToString())
            .Concat(methodArguments.Select(x => x.ToString()))
            .ToList();

        var asyncKeyword = isAsync ? "async" : "";
        var awaitKeyword = isAsync ? "await" : "";
        
        _builder.Append($$"""
                                         
                                         
                                      {{
                                          GetGroupVariableName()
                                      }}.Map{{httpMethod}}("{{endpoint}}", {{
                                          asyncKeyword
                                      }} ({{
                                          string.Join(",\n\t\t\t\t", combinedServices)
                                      }}) => {
                                          var controller = new {{
                                              _currentController
                                          }}({{
                                              string.Join(",\n\t\t\t\t", controllerServices.Select(x => x.Name))
                                          }});
                                        
                                          var result = {{
                                              awaitKeyword
                                              }} controller.{{methodName}}({{
                                              string.Join(",\n\t\t\t\t", methodArguments.Select(x => x.Name))
                                          }});
                                          
                                          return MinimalControllers.Converters.TryConvertToIResult(result, out var iResult)
                                              ? iResult
                                              : Microsoft.AspNetCore.Http.Results.Ok(result);
                                      })
                                      .WithOpenApi();
                          """);
        return this;
    }
    
    public string Build() =>
        _builder.AppendLine("\n\t\t\treturn app;\n\t\t}\n\t}\n}").ToString();

    private static string RandomString(string input, int item, int length = 32)
    {
        var random = new Random(Seed: input.GetHashCode());
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        return $"{NormalizeName(input)}_{item}_{new string(
            Enumerable
                .Repeat(chars, length)
                .Select(s => s[random
                    .Next(s.Length)])
                .ToArray())}";
    }

    private static string NormalizeName(string name) =>
        name
            .Split(' ')
            .Last()
            .Split('.')
            .Last()
            .Split('<')
            .First()
            .Replace("<", "")
            .Replace(">", "")
            .ToLower(CultureInfo.InvariantCulture);
}