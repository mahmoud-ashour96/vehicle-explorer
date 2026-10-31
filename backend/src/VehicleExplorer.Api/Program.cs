using VehicleExplorer.Api.Middleware;
using VehicleExplorer.Application;
using VehicleExplorer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// The Docker image copies the built Angular app into wwwroot, so this app serves the UI as well as the API.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Any other URL is an Angular page.
app.MapFallbackToFile("index.html");

app.Run();
