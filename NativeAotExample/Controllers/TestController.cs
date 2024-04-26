using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using NativeAotExample.Model;

namespace NativeAotExample.Controllers;

[MinimalControllers.ApiController]
[MinimalControllers.Route("[controller]/works")]
public class TestController : Microsoft.AspNetCore.Mvc.Controller
{
    public TestController(HttpClient httpClient)
    {
    }

    [MinimalControllers.HttpGet]
    public IActionResult Test([FromServices]Random random)
    {
        var number = random.Next();
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

    [HttpGet("/{apple}")]
    public async Task<IActionResult> Test4([FromHeader(Name = "banana")] string header, [FromBody] string banana, [FromRoute(Name = "apple")] string apple, [FromServices] string lol)
    {
        return BadRequest();
    }
}