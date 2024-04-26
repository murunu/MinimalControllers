using Microsoft.AspNetCore.Mvc;

namespace NativeAotExample.Controllers;

[ApiController]
[Route("/[controller]/apple/[action]")]
public class ThirdController : Controller
{
    public ThirdController(Random random)
    {
    }
    
    [HttpGet("/banana/{max:int}")]
    public IActionResult GetAll([FromServices] Random random, [FromRoute(Name = "max")] int max)
    {
        return Ok($"Test, {random.Next(0, max)}");
    }
}