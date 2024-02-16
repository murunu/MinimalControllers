using Microsoft.AspNetCore.Mvc;
using NativeAotExample.Model;

namespace NativeAotExample.Controllers;

[MinimalControllers.ApiController]
public class TestController : Microsoft.AspNetCore.Mvc.Controller
{
    public TestController(ILogger<TestController> logger, WeatherForecast weatherForecast)
    {
        
    }
    
    [MinimalControllers.HttpGet]
    public async Task<IActionResult> Test(string test)
    {
        return Ok(test);
    }
    
    [MinimalControllers.HttpGet]
    public async Task<string> Test2()
    {
        return "Hello World!";
    }

    [MinimalControllers.HttpPut]
    public async Task<string> Test3()
    {
        return "";
    }
}