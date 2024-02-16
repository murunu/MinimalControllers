using MinimalControllers;
using NativeAotExample.Model;

namespace NativeAotExample.Controllers;

[ApiController]
public class TestController : Microsoft.AspNetCore.Mvc.Controller
{
    public TestController(ILogger<TestController> logger, WeatherForecast weatherForecast)
    {
        
    }
    
    [HttpGet]
    public async Task<string> Test()
    {
        return "Hello World!";
    }
    
    [HttpGet]
    public async Task<string> Test2()
    {
        return "Hello World!";
    }

    [HttpPut]
    public async Task<string> Test3()
    {
        return "";
    }
}