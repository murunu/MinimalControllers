using MinimalControllers;

namespace NativeAotExample.Controllers;

[Controller]
[Route("/RandomAssController")]
public class RandomController
{
    private readonly Random _random;
    
    public RandomController(Random random)
    {
        _random = random;
    }
    
    [HttpGet]
    public string GetRandomNumber()
    {
        return _random.Next().ToString();
    }
    
    [HttpPost]
    public string GetRandomNumber2()
    {
        return _random.Next().ToString();
    }

    [Microsoft.AspNetCore.Mvc.HttpGet]
    public string GetAnotherThing([Microsoft.AspNetCore.Mvc.FromQuery] int skip, [Microsoft.AspNetCore.Mvc.FromQuery] int top)
    {
        return string.Empty;
    }
}