using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Infrastructure.Vpic;

namespace VehicleExplorer.Tests.Infrastructure;

public class VpicVehicleDataSourceTests
{
    [Fact]
    public async Task GetMakesAsync_MapsResultsTrimsNamesAndSkipsBlanks()
    {
        var (dataSource, _) = CreateDataSource(JsonResponse("""
            {"Count":3,"Results":[
              {"Make_ID":448,"Make_Name":" TOYOTA "},
              {"Make_ID":1,"Make_Name":""},
              {"Make_ID":474,"Make_Name":"HONDA"}
            ]}
            """));

        var makes = await dataSource.GetMakesAsync();

        Assert.Equal(["TOYOTA", "HONDA"], makes.Select(m => m.Name));
        Assert.Equal([448, 474], makes.Select(m => m.Id));
    }

    [Fact]
    public async Task GetModelsAsync_RemovesDuplicateModels()
    {
        var (dataSource, _) = CreateDataSource(JsonResponse("""
            {"Results":[
              {"Model_ID":1861,"Model_Name":"Accord"},
              {"Model_ID":1861,"Model_Name":"Accord"},
              {"Model_ID":1863,"Model_Name":"Civic"}
            ]}
            """));

        var models = await dataSource.GetModelsAsync(474, 2015, null);

        Assert.Equal(["Accord", "Civic"], models.Select(m => m.Name));
    }

    [Fact]
    public async Task GetModelsAsync_WithVehicleType_EscapesItInTheUrl()
    {
        var (dataSource, handler) = CreateDataSource(JsonResponse("""{"Results":[]}"""));

        await dataSource.GetModelsAsync(474, 2015, "Passenger Car");

        Assert.Equal(
            "https://vpic.test/api/vehicles/GetModelsForMakeIdYear/makeId/474/modelyear/2015/vehicletype/Passenger%20Car?format=json",
            handler.LastRequestUrl);
    }

    [Fact]
    public async Task GetModelsAsync_WithoutVehicleType_UsesMakeAndYearEndpoint()
    {
        var (dataSource, handler) = CreateDataSource(JsonResponse("""{"Results":[]}"""));

        await dataSource.GetModelsAsync(474, 2015, null);

        Assert.Equal(
            "https://vpic.test/api/vehicles/GetModelsForMakeIdYear/makeId/474/modelyear/2015?format=json",
            handler.LastRequestUrl);
    }

    [Fact]
    public async Task ServerError_ThrowsExternalServiceException()
    {
        var (dataSource, _) = CreateDataSource(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<ExternalServiceException>(() => dataSource.GetMakesAsync());
    }

    [Fact]
    public async Task HtmlInsteadOfJson_ThrowsExternalServiceException()
    {
        var (dataSource, _) = CreateDataSource(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Service Unavailable</html>", Encoding.UTF8, "application/json"),
        });

        await Assert.ThrowsAsync<ExternalServiceException>(() => dataSource.GetMakesAsync());
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static (VpicVehicleDataSource DataSource, StubHttpHandler Handler) CreateDataSource(HttpResponseMessage response)
    {
        var handler = new StubHttpHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://vpic.test/api/vehicles/") };
        var dataSource = new VpicVehicleDataSource(httpClient, NullLogger<VpicVehicleDataSource>.Instance);

        return (dataSource, handler);
    }

    private class StubHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public string? LastRequestUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUrl = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(_response);
        }
    }
}
