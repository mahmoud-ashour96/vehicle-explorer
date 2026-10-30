using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleExplorer.Api.Vpic;

namespace VehicleExplorer.Api.Tests;

public sealed class VpicClientTests
{
    [Fact]
    public async Task GetMakesAsync_TrimsNamesSkipsBlanksAndSortsByName()
    {
        var (client, _) = CreateClient(Json("""
            {"Count":3,"Results":[
              {"Make_ID":2,"Make_Name":" TOYOTA "},
              {"Make_ID":3,"Make_Name":""},
              {"Make_ID":1,"Make_Name":"honda"}
            ]}
            """));

        var makes = await client.GetMakesAsync(CancellationToken.None);

        Assert.Equal(["honda", "TOYOTA"], makes.Select(m => m.Name));
        Assert.Equal([1, 2], makes.Select(m => m.Id));
    }

    [Fact]
    public async Task GetModelsAsync_EscapesVehicleTypeIntoThePath()
    {
        var (client, handler) = CreateClient(Json("""{"Results":[]}"""));

        await client.GetModelsAsync(474, 2015, "Passenger Car", CancellationToken.None);

        Assert.Equal(
            "https://vpic.test/api/vehicles/GetModelsForMakeIdYear/makeId/474/modelyear/2015/vehicletype/Passenger%20Car?format=json",
            handler.LastRequestUri);
    }

    [Fact]
    public async Task GetModelsAsync_WithoutVehicleType_UsesMakeAndYearEndpoint()
    {
        var (client, handler) = CreateClient(Json("""{"Results":[]}"""));

        await client.GetModelsAsync(474, 2015, vehicleType: null, CancellationToken.None);

        Assert.Equal(
            "https://vpic.test/api/vehicles/GetModelsForMakeIdYear/makeId/474/modelyear/2015?format=json",
            handler.LastRequestUri);
    }

    [Fact]
    public async Task GetModelsAsync_RemovesDuplicateModelIds()
    {
        var (client, _) = CreateClient(Json("""
            {"Results":[
              {"Model_ID":1861,"Model_Name":"Accord"},
              {"Model_ID":1861,"Model_Name":"Accord"},
              {"Model_ID":1863,"Model_Name":"Civic"}
            ]}
            """));

        var models = await client.GetModelsAsync(474, 2015, null, CancellationToken.None);

        Assert.Equal(["Accord", "Civic"], models.Select(m => m.Name));
    }

    [Fact]
    public async Task ServerError_IsWrappedInUpstreamServiceException()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.GetMakesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NonJsonBody_IsWrappedInUpstreamServiceException()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Service Unavailable</html>", Encoding.UTF8, "application/json"),
        });

        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.GetMakesAsync(CancellationToken.None));
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (VpicClient Client, StubHandler Handler) CreateClient(HttpResponseMessage response)
    {
        var handler = new StubHandler(response);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://vpic.test/api/vehicles/") };
        return (new VpicClient(http, NullLogger<VpicClient>.Instance), handler);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequestUri = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(response);
        }
    }
}
