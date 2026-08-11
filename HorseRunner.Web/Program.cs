var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl =
            context.File.Name == "index.html" ? "no-cache" : "public,max-age=3600";
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
app.MapFallbackToFile("index.html");

app.Run();
