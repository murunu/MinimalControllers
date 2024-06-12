# Murunu.MinimalControllers
Source generator to create minimal API endpoints from MVC controllers.

## Features
- **Automatic Endpoint Generation**: Converts MVC controllers to minimal API endpoints automatically.
- **Dependency Injection**: Handles all dependency injection configurations seamlessly.
- **Asynchronous Support**: Fully supports asynchronous operations.
- **Custom Converters**: Allows custom converters to convert a type to an `IResult`.

## Getting Started
Murunu.MinimalControllers is installed from [NuGet](https://www.nuget.org/packages/Murunu.MinimalControllers).

```bash
dotnet add package Murunu.MinimalControllers
```

## Usage
To use the source generator, add the following code to your startup class:

```csharp
app.UseControllers();
```

Make sure to remove any calls to `app.MapControllers()` or similar methods that map controller endpoints, as the source generator will handle this for you.

## Example
Here is an example of a simple MVC controller:
```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherForecastController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        return await _weatherService.GetForecastAsync();
    }
}
```

After adding `app.UseControllers()`, the above controller will automatically be converted to a minimal API endpoint.

## Custom Converters
A default converters exists to convert from an IActionResult to an IResult.

You can add custom converters to convert a type to an IResult. Here is an example:
```csharp
[MinimalConverter]
public static bool Converter(object value, out IResult result)
{
    // Do something with value.
    
    result = Results.Ok();
    return true;
}
```
You can add as many converters as needed by annotating them with [MinimalConverter].

If no converter matches the result type, the endpoint will just return the result as is.

## Got a Question or Problem?
If you encounter any issues or have questions, feel free to reach out!  
You can [Create an Issue](https://github.com/murunu/MinimalControllers/issues/new/choose) on Github  

We appreciate your contributions and feedback!

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request.

## Links
 - [GitHub Repository](https://github.com/murunu/MinimalControllers)
 - [Issue Tracker](https://github.com/murunu/MinimalControllers/issues)
