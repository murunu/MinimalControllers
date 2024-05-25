using Microsoft.AspNetCore.Mvc;

namespace NativeAotExample.Controllers;

[Route("/[controller]/apple/[action]")]
public class ThirdController : Controller
    // : GenericController<Random>
{
    public ThirdController(Random aaaaaaaaaaaa)
    {
    }
    
    [HttpGet("/banana/{max:int}")]
    public IActionResult GetAllInternal([FromServices] Random random, [FromRoute(Name = "max")] int max)
    {
        return Ok($"Test, {random.Next(0, max)}");
    }
    
    // [HttpPost("/apple")]
    // public override async Task<IActionResult> Post()
    // {
    //     return Ok();
    // }
}

public abstract class GenericController<TOut> : BaseController
where TOut : new()
{
    [HttpGet("/Banana")]
    public async Task<IActionResult> GetAll([FromRoute(Name = "apple")] [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] string x)
    {
        return Ok(new TOut());
    }

    [HttpPost]
    public virtual async Task<IActionResult> Post() => Ok();
}

[ApiController]
public abstract class BaseController : Controller
{
    
}