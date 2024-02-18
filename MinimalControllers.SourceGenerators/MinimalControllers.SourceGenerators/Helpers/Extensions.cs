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

    private const string ConvertToIResultMethod = 
        """
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
                        case Microsoft.AspNetCore.Mvc.ChallengeResult challengeResult:
                            result = Microsoft.AspNetCore.Http.Results.Challenge();
                            return true;
                        case Microsoft.AspNetCore.Mvc.JsonResult jsonResult:
                            result = Microsoft.AspNetCore.Http.Results.Ok(jsonResult.Value);
                            return true;
                        case Microsoft.AspNetCore.Mvc.AcceptedResult acceptedResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(acceptedResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.AcceptedAtActionResult acceptedAtActionResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(acceptedAtActionResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult acceptedAtRouteResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(acceptedAtRouteResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.CreatedAtActionResult createdAtActionResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(createdAtActionResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.CreatedAtRouteResult createdAtRouteResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(createdAtRouteResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.CreatedResult createdResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(createdResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.NoContentResult noContentResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(noContentResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.UnsupportedMediaTypeResult unsupportedMediaTypeResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(unsupportedMediaTypeResult.StatusCode);
                            return true;
                        case Microsoft.AspNetCore.Mvc.PartialViewResult partialViewResult:
                            result = Microsoft.AspNetCore.Http.Results.Content(partialViewResult.ViewName);
                            return true;
                        case Microsoft.AspNetCore.Mvc.ViewResult viewResult:
                            result = Microsoft.AspNetCore.Http.Results.Content(viewResult.ViewName);
                            return true;
                        case Microsoft.AspNetCore.Mvc.FileResult fileResult:
                            result = Microsoft.AspNetCore.Http.Results.File(fileResult.FileDownloadName);
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