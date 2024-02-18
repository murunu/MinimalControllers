using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using NativeAotExample.Model;

namespace NativeAotExample.Controllers;

[MinimalControllers.ApiController]
[MinimalControllers.Route("[controller]/works")]
public class TestController : Microsoft.AspNetCore.Mvc.Controller
{
    public TestController()
    {
    }

    [MinimalControllers.HttpGet]
    public IActionResult Test()
    {
        return Ok();
    }

    [MinimalControllers.HttpGet]
    public string Test2()
    {
        return "Hello World!";
    }

    [MinimalControllers.HttpGet]
    public IActionResult Test3()
    {
        return Challenge();
    }
}