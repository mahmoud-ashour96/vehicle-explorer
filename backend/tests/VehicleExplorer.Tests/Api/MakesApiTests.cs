using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleExplorer.Api.BackgroundServices;
using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Api;

/// <summary>
/// Runs requests through the real HTTP pipeline (routing, controllers, error handling) with vPIC replaced by a fake.
/// </summary>
public class MakesApiTests : IDisposable
{
    private readonly FakeVehicleDataSource _dataSource = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MakesApiTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IVehicleDataSource>();
                services.AddSingleton<IVehicleDataSource>(_dataSource);

                // The startup warm-up would call the fake in the background and make call counts unpredictable.
                services.Remove(services.Single(d => d.ImplementationType == typeof(MakesCacheWarmer)));
            }));

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetMakes_Returns200WithSortedMakesAndCacheHeader()
    {
        var response = await _client.GetAsync("/api/makes");

        response.EnsureSuccessStatusCode();
        Assert.Equal(TimeSpan.FromHours(1), response.Headers.CacheControl?.MaxAge);

        var makes = await response.Content.ReadFromJsonAsync<List<MakeDto>>();
        Assert.Equal(["HONDA", "TOYOTA"], makes!.Select(m => m.Name));
    }

    [Fact]
    public async Task GetModelYears_ReturnsNewestYearFirst()
    {
        var years = await _client.GetFromJsonAsync<List<int>>("/api/model-years");

        Assert.Equal(DateTime.UtcNow.Year + 1, years![0]);
    }

    [Fact]
    public async Task GetModels_PassesFiltersToDataSource()
    {
        var models = await _client.GetFromJsonAsync<List<VehicleModelDto>>(
            "/api/makes/474/models?year=2015&vehicleType=Passenger%20Car");

        Assert.Equal(["Accord", "Civic"], models!.Select(m => m.Name));
        Assert.Equal((474, 2015, "Passenger Car"), _dataSource.LastModelsRequest);
    }

    [Theory]
    [InlineData("/api/makes/474/models")]
    [InlineData("/api/makes/474/models?year=1900")]
    [InlineData("/api/makes/474/models?year=2015&vehicleType=..%2F..%2Fsecret")]
    [InlineData("/api/makes/0/vehicle-types")]
    public async Task InvalidInput_Returns400ValidationProblem(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotEmpty(problem!.Errors);
        Assert.Null(_dataSource.LastModelsRequest);
    }

    [Fact]
    public async Task DataSourceFailure_Returns502Problem()
    {
        _dataSource.ShouldFail = true;

        var response = await _client.GetAsync("/api/makes/474/vehicle-types");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Vehicle data is temporarily unavailable", problem!.Title);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
