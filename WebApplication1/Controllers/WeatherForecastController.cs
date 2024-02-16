
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[Route("[controller]")]
[ApiController]
public class WeatherForecastController
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}

public class HttpGetAttribute : HttpMethodAttribute
{
    private static readonly IEnumerable<string> _supportedMethods = new[] { "GET" };

    public HttpGetAttribute()
        : base(_supportedMethods)
    {
    }
}

public abstract class HttpMethodAttribute : Attribute
{
    [StringSyntax("Route")]
    public string? Template { get; }
    public IEnumerable<string> HttpMethods => _httpMethods;
    
    public string? Name { get; set; }
    
    private readonly List<string> _httpMethods;

    private int? _order;

    public HttpMethodAttribute(IEnumerable<string> httpMethods, [StringSyntax("Route")] string? template = null)
    {
        if (httpMethods == null)
        {
            throw new ArgumentNullException(nameof(httpMethods));
        }

        _httpMethods = httpMethods.ToList();
        Template = template;
    }
}

public class RouteAttribute : Attribute
{
    [StringSyntax("Route")]
    public string Template { get; }
    public RouteAttribute([StringSyntax("Route")] string template)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));;
    }
}