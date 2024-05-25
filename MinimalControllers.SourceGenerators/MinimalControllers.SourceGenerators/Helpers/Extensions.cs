using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MinimalControllers.SourceGenerators.Helpers;
using MinimalControllers.SourceGenerators.Helpers.CompilationHelpers;

namespace MinimalControllers.SourceGenerators.Helpers;

public static class Extensions
{
    public static void AddConvertToIResultMethod(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.attributeFound)
            .Select((t, _) => t.classDeclarationSyntax);
        
        // Generate the source code.
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
        
        // context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
        //     "Extensions.g.cs",
        //     SourceText.From(ConvertToIResultMethod, Encoding.UTF8)));
    }
    
    private static (
        ClassDeclarationSyntax classDeclarationSyntax,
        bool attributeFound
        ) GetClassDeclarationForSourceGen(
            GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
    
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
    
        if (symbol == null)
        {
            return (null, false);
        }
        
        return (classDeclarationSyntax, GetConverterAttribute(symbol));
    }
    
    private static bool GetConverterAttribute(INamedTypeSymbol symbol)
    {
        return symbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Any(x => 
                x.GetAttributes()
                    .Any(y => 
                        y
                            .ToString()
                            .Contains("MinimalConverter")));
    }
    
    private static void GenerateCode(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        var source = new StringBuilder();
        
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (symbol == null)
                continue;
            
            GenerateMethods(symbol, source, context);
        }
        
        context.AddSource("Extensions.g.cs", 
            SourceText.From(
                ConvertToIResultMethod(
                    source.ToString()), 
                Encoding.UTF8));
    }

    private static void GenerateMethods(INamedTypeSymbol symbol, StringBuilder source, SourceProductionContext context)
    {
        var methods = symbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x =>
                x.GetAttributes()
                    .Any(y =>
                        y
                            .ToString()
                            .Contains("MinimalConverter")));

        foreach (var method in methods)
        {
            GenerateSingleMethod(method, source, context);
        }
    }

    private static void GenerateSingleMethod(IMethodSymbol symbol, StringBuilder source, SourceProductionContext context)
    {
        if (!symbol.IsStatic ||
            symbol.ReturnType.Name != "Boolean" ||
            symbol.Parameters.Length < 1 ||
            symbol.Parameters[0].Type.Name != "Object" ||
            symbol.Parameters[1].Type.Name != "IResult" ||
            symbol.Parameters[1].RefKind != RefKind.Out)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("CS0708", 
                $"MinimalConverter {symbol} should be static",
                $"MinimalConverter {symbol} should be static",
                "Static",
                DiagnosticSeverity.Error,
                true,
                $"{symbol} should be static"),
                symbol.Locations.First()));

            return;
        }

        var name = symbol.ToString().Split('(').First();

        source.AppendLine(
            $$"""
            
                        if({{
                            name
                        }}(actionResult, out var {{
                            name.Replace(".", "").ToLower()
                        }}Result))
                        {
                            result = {{name.Replace(".", "").ToLower()}}Result;
                            return true;
                        }
            """);
    }

    private static string ConvertToIResultMethod(string customConverters) => 
        $$"""
        namespace MinimalControllers
        {
            public static class Converters
            {
                public static bool TryConvertToIResult(
                    object actionResult,
                    out Microsoft.AspNetCore.Http.IResult? result)
                {
                    {{customConverters}}
                    
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
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(acceptedResult.StatusCode ?? 200);
                            return true;
                        case Microsoft.AspNetCore.Mvc.AcceptedAtActionResult acceptedAtActionResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(acceptedAtActionResult.StatusCode ?? 200);
                            return true;
                        case Microsoft.AspNetCore.Mvc.AcceptedAtRouteResult acceptedAtRouteResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(acceptedAtRouteResult.StatusCode ?? 200);
                            return true;
                        case Microsoft.AspNetCore.Mvc.CreatedAtActionResult createdAtActionResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(createdAtActionResult.StatusCode ?? 200);
                            return true;
                        case Microsoft.AspNetCore.Mvc.CreatedAtRouteResult createdAtRouteResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(createdAtRouteResult.StatusCode ?? 200);
                            return true;
                        case Microsoft.AspNetCore.Mvc.CreatedResult createdResult:
                            result = Microsoft.AspNetCore.Http.Results.StatusCode(createdResult.StatusCode ?? 200);
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
                        //case Amazon.Lambda.Annotations.APIGateway.HttpResults httpResults:
                          //  result = Microsoft.AspNetCore.Http.Results.Json(
                            //    GetRawHttpResultsBody(httpResults), 
                              //  statusCode: (int)httpResults.StatusCode);
                            //return true;
                        default:
                            result = null;
                            return false;
                    }
                }
                
                //[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "_rawBody")]
                //extern static ref object GetRawHttpResultsBody(Amazon.Lambda.Annotations.APIGateway.HttpResults @this);
            }
        }
        """;
}