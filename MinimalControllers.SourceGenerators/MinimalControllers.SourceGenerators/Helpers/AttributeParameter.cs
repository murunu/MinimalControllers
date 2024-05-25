using System.Collections.Generic;
using System.Linq;

namespace MinimalControllers.SourceGenerators.Helpers;

public class AttributeParameter(string name, Dictionary<string, string>? arguments = null)
{
    public string Name { get; } = name;
    public Dictionary<string, string>? Arguments { get; } = arguments;

    public override string ToString()
    {
        return Arguments is { Count: > 0 } 
            ? $"[{Name}({string.Join(", ", Arguments
                .Select(x => $"{x.Key} = {x.Value}"))})]"
            : $"[{Name}]";
    }
}