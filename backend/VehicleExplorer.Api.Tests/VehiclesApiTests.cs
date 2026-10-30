using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleExplorer.Api.Models;
using VehicleExplorer.Api.Services;
using VehicleExplorer.Api.Vpic;

namespace VehicleExplorer.Api.Tests;

/// <summary>End-to-end through the real HTTP pipeline, with vPIC replaced by a fake.</summary>
public sealed class VehiclesApiTests : IDisposable
{
    private readonly FakeVpicClient _vpic = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public VehiclesApiTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IVpicClient>();
                services.AddSingleton<IVpicClient>(_vpic);

                // The warm-up would call the fake in the background and make call counts racy.
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
    public async Task GetMakes_ReturnsMakesWithCacheHeader()
    {
        var response = await _client.GetAsync("/api/makes");

        response.EnsureSuccessStatusCode();
        Assert.Equal(TimeSpan.FromHours(1), response.Headers.CacheControl?.MaxAge);
        var makes = await response.Content.ReadFromJsonAsync<List<Make>>();
        Assert.Equal([new Make(474, "HONDA"), new Make(448, "TOYOTA")], makes);
    }

    [Fact]
    public async Task GetMakes_RepeatedRequestsHitVpicOnce()
    {
        await _client.GetAsync("/api/makes");
        await _client.GetAsync("/api/makes");

        Assert.Equal(1, _vpic.MakesCalls);
    }

    [Fact]
    public async Task GetModels_PassesMakeYearAndVehicleTypeToVpic()
    {
        var models = await _client.GetFromJsonAsync<List<VehicleModel>>(
            "/api/makes/474/models?year=2015&vehicleType=Passenger%20Car");

        Assert.Equal([new VehicleModel(1861, "Accord")], models);
        Assert.Equal((474, 2015, "Passenger Car"), _vpic.LastModelsQuery);
    }

    [Theory]
    [InlineData("/api/makes/474/models?year=1900")]
    [InlineData("/api/makes/474/models")]
    [InlineData("/api/makes/474/models?year=2015&vehicleType=..%2F..%2Fsecret")]
    public async Task GetModels_InvalidInput_Returns400WithoutCallingVpic(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
        Assert.Null(_vpic.LastModelsQuery);
    }

    [Fact]
    public async Task VpicFailure_Returns502ProblemDetails()
    {
        _vpic.Fail = true;

        var response = await _client.GetAsync("/api/makes/474/vehicle-types");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Vehicle data is temporarily unavailable", problem?.Title);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404InsteadOfTheSpa()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class FakeVpicClient : IVpicClient
    {
        public bool Fail { get; set; }
        public int MakesCalls { get; private set; }
        public (int MakeId, int Year, string? VehicleType)? LastModelsQuery { get; private set; }

        public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken ct)
        {
            MakesCalls++;
            return Respond<Make>([new(474, "HONDA"), new(448, "TOYOTA")]);
        }

        public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken ct) =>
            Respond<VehicleType>([new(2, "Passenger Car")]);

        public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
            int makeId, int year, string? vehicleType, CancellationToken ct)
        {
            LastModelsQuery = (makeId, year, vehicleType);
            return Respond<VehicleModel>([new(1861, "Accord")]);
        }

        private Task<IReadOnlyList<T>> Respond<T>(IReadOnlyList<T> result) =>
            Fail ? throw new UpstreamServiceException("vPIC is down") : Task.FromResult(result);
    }
}
