using Microsoft.AspNetCore.Mvc;

namespace NativeAotExample.Controllers;

[ApiController]
[Route("/[controller]/apple/[action]")]
public class ThirdController : Controller
{
    public ThirdController(Random random)
    {
    }
    
    [HttpGet("/banana/{id}")]
    public IActionResult GetAll([FromServices] Random random)
    {
        return Ok();
    }
}