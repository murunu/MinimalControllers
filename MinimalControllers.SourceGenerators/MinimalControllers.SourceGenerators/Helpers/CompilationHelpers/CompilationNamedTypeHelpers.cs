using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MinimalControllers.SourceGenerators.Helpers.CompilationHelpers;

public static class CompilationNamedTypeHelpers
{
    public static IEnumerable<ArgumentParameter> GetControllerServices(INamedTypeSymbol symbol)
    {
        var ctor = symbol.GetMembers().OfType<IMethodSymbol>()
            .FirstOrDefault(x => x.MethodKind == MethodKind.Constructor);

        if (ctor == null)
            return [];

        var result = CompilationMethodHelpers.GetMethodArguments(ctor).ToList();
        
        foreach (var argumentParameter in result.Where(x => x.Attribute == null || !x.Attribute.Any()))
        {
            argumentParameter.Attribute ??= [];
            
            argumentParameter.Attribute.Add(
                new AttributeParameter("Microsoft.AspNetCore.Mvc.FromServicesAttribute"));
        }
        
        return result;
    }

}