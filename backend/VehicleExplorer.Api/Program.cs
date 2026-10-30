using VehicleExplorer.Api.Infrastructure;
using VehicleExplorer.Api.Services;
using VehicleExplorer.Api.Vpic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache(options => options.SizeLimit = 5_000);
builder.Services.AddScoped<VehicleCatalog>();
builder.Services.AddHostedService<MakesCacheWarmer>();

var vpicBaseUrl = builder.Configuration["Vpic:BaseUrl"]
    ?? throw new InvalidOperationException("Missing configuration value 'Vpic:BaseUrl'.");

builder.Services
    .AddHttpClient<IVpicClient, VpicClient>(client => client.BaseAddress = new Uri(vpicBaseUrl))
    .AddStandardResilienceHandler(); // retries with backoff, per-attempt + total timeouts, circuit breaker

var app = builder.Build();

app.UseResponseCompression();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// In the Docker image the built Angular app lives in wwwroot, so one container serves both UI and API.
// Angular emits content-hashed file names, so those can be cached for a year; index.html must
// always be revalidated so a new deployment is picked up immediately.
var spaFiles = new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl =
        ctx.File.Name == "index.html" ? "no-cache" : "public,max-age=31536000,immutable",
};
app.UseDefaultFiles();
app.UseStaticFiles(spaFiles);

app.MapControllers();
app.MapHealthChecks("/health");

// Unknown /api routes are real 404s; everything else is an Angular client-side route.
app.MapFallback("api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html", spaFiles);

app.Run();
