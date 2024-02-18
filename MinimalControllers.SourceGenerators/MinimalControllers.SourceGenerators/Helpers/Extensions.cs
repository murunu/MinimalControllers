using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MinimalControllers.SourceGenerators.Helpers;

public static class Extensions
{
    public static void AddConvertToIResultMethod(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "Extensions.g.cs",
            SourceText.From(ConvertToIResultMethod, Encoding.UTF8)));
    }

    private const string ConvertToIResultMethod = """
                                                  namespace MinimalControllers
                                                  {
                                                      public static class Converters
                                                      {
                                                          public static bool TryConvertToIResult(
                                                              object actionResult,
                                                              out Microsoft.AspNetCore.Http.IResult? result)
                                                          {
                                                              switch (actionResult)
                                                              {
                                                                  case Microsoft.AspNetCore.Mvc.OkResult okResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.StatusCode(okResult.StatusCode);
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.OkObjectResult okObjectResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.Ok(okObjectResult.Value);
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.BadRequestResult badRequestResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.BadRequest();
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.BadRequestObjectResult badRequestObjectResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.BadRequest(badRequestObjectResult.Value);
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.NotFoundResult notFoundResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.NotFound();
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.UnauthorizedResult unauthorizedResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.Unauthorized();
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.ContentResult contentResult:
                                                                      result = Microsoft.AspNetCore.Http.Results.Content(contentResult.Content, contentResult.ContentType);
                                                                      return true;
                                                                  case Microsoft.AspNetCore.Mvc.JsonResult jsonResult:
                                                                      // Return an Ok Result since Results.Json does not work with NativeAOT
                                                                      result = Microsoft.AspNetCore.Http.Results.Ok(jsonResult.Value);
                                                                      return true;
                                                                  default:
                                                                      result = null;
                                                                      return false;
                                                              }
                                                          }
                                                      }
                                                  }
                                                  """;
}