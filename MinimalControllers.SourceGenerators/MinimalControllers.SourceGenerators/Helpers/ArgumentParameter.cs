using System.Collections.Generic;
using System.Linq;

namespace MinimalControllers.SourceGenerators.Helpers;

public class ArgumentParameter(string name, string typeName, List<AttributeParameter>? attribute = null)
{
    public string Name { get; set; } = name;
    public string TypeName { get; } = typeName;
    public List<AttributeParameter>? Attribute { get; set; } = attribute;

    public override string ToString()
    {
        return Attribute == null 
            ? $"{TypeName} {Name}" 
            : $"{string.Join(" ", Attribute.Select(x => x.ToString()))} {TypeName} {Name}";
    }
}