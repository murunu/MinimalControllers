# Murunu.MinimalControllers
This is a source generator that generates minimal controllers for ASP.NET Core 6.0+ applications.

## Usage
To use the source generator, add the following code to your startup class:

```csharp
app.UseControllers();
```

Make sure to remove the call to `app.MapControllers()` as the source generator will generate the endpoints for you.