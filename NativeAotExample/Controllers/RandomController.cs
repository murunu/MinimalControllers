using MinimalControllers;

namespace NativeAotExample.Controllers;

[Controller]
[Route("/RandomAssController")]
public class RandomController
{
    private readonly Random _random;
    
    public RandomController(ILogger<RandomController> logger, Random random)
    {
        _random = random;
    }
    
    [HttpGet]
    public async Task<string> GetRandomNumber(string seed)
    {
        return _random.Next().ToString();
    }
}