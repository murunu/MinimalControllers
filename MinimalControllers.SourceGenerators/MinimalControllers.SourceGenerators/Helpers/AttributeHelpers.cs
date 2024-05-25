using System;
using System.Collections.Generic;
using System.Linq;

namespace MinimalControllers.SourceGenerators.Helpers;

public static class AttributeHelpers
{
    public static bool AttributeEndsWithAny(string attribute, IEnumerable<string> names)
    {
        return names.Any(name =>
        {
            var result = attribute.Split('(').FirstOrDefault();
            
            if(string.IsNullOrEmpty(result))
            {
                return false;
            }

            if (result.EndsWith("Attribute"))
            {
                result = result.Replace("Attribute", "");
            }

            return result.EndsWith(name, StringComparison.OrdinalIgnoreCase);
        });
    }
}