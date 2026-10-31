using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Api;

/// <summary>
/// Calls the real API in memory, with the vPIC data source replaced by a fake.
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
            }));

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetMakes_ReturnsMakes()
    {
        var makes = await _client.GetFromJsonAsync<List<MakeDto>>("/api/makes");

        Assert.Equal(["HONDA", "TOYOTA"], makes!.Select(m => m.Name));
    }

    [Fact]
    public async Task GetModelYears_StartsWithNextYear()
    {
        var years = await _client.GetFromJsonAsync<List<int>>("/api/model-years");

        Assert.Equal(DateTime.UtcNow.Year + 1, years![0]);
    }

    [Fact]
    public async Task GetModels_PassesQueryToDataSource()
    {
        var models = await _client.GetFromJsonAsync<List<VehicleModelDto>>(
            "/api/makes/474/models?year=2015&vehicleType=Passenger%20Car");

        Assert.Equal(["Accord", "Civic", "CR-Z"], models!.Select(m => m.Name));
        Assert.Equal((474, 2015, "Passenger Car"), _dataSource.LastModelsRequest);
    }

    [Theory]
    [InlineData("/api/makes/474/models")]
    [InlineData("/api/makes/474/models?year=1900")]
    public async Task GetModels_InvalidYear_Returns400(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("year", problem!.Errors.Keys);
    }

    [Fact]
    public async Task DataSourceFailure_Returns502()
    {
        _dataSource.ShouldFail = true;

        var response = await _client.GetAsync("/api/makes/474/vehicle-types");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
