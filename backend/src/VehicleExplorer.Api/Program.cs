using VehicleExplorer.Api.BackgroundServices;
using VehicleExplorer.Api.Middleware;
using VehicleExplorer.Application;
using VehicleExplorer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Web
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddHostedService<MakesCacheWarmer>();

var app = builder.Build();

app.UseResponseCompression();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// In the Docker image the built Angular app is copied to wwwroot, so one container serves the UI and the API.
// Angular file names contain a content hash, so they can be cached for a year. index.html is always
// revalidated so users get a new deployment straight away.
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl =
        context.File.Name == "index.html" ? "no-cache" : "public,max-age=31536000,immutable",
};
app.UseDefaultFiles();
app.UseStaticFiles(staticFileOptions);

app.MapControllers();
app.MapHealthChecks("/health");

// Unknown /api routes return 404; any other path is an Angular route and gets index.html.
app.MapFallback("api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html", staticFileOptions);

app.Run();
