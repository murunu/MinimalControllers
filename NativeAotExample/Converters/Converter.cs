using MinimalControllers;

namespace NativeAotExample.Converters;

public class Converter
{
    [MinimalConverter]
    public static bool TryConvert(object item, out IResult result)
    {
        result = Results.Ok("Hello from custom converter");
        return true;
    }

    [MinimalConverter]
    public static bool TryConvert2(object item, out IResult result)
    {
        result = Results.BadRequest();

        return true;
    }
}