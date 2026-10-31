using System.Net;
using System.Text;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Infrastructure.Vpic;

namespace VehicleExplorer.Tests.Infrastructure;

public class VpicVehicleDataSourceTests
{
    [Fact]
    public async Task GetMakesAsync_MapsResultsAndTrimsNames()
    {
        var (dataSource, _) = CreateDataSource("""
            {"Results":[{"Make_ID":448,"Make_Name":" TOYOTA "},{"Make_ID":474,"Make_Name":"HONDA"}]}
            """);

        var makes = await dataSource.GetMakesAsync();

        Assert.Equal([448, 474], makes.Select(m => m.Id));
        Assert.Equal(["TOYOTA", "HONDA"], makes.Select(m => m.Name));
    }

    [Fact]
    public async Task GetModelsAsync_RemovesDuplicateModels()
    {
        var (dataSource, _) = CreateDataSource("""
            {"Results":[
              {"Model_ID":1861,"Model_Name":"Accord"},
              {"Model_ID":1861,"Model_Name":"Accord"},
              {"Model_ID":1863,"Model_Name":"Civic"}
            ]}
            """);

        var models = await dataSource.GetModelsAsync(474, 2015, null);

        Assert.Equal(["Accord", "Civic"], models.Select(m => m.Name));
    }

    [Fact]
    public async Task GetModelsAsync_WithoutVehicleType_CallsMakeAndYearEndpoint()
    {
        var (dataSource, handler) = CreateDataSource("""{"Results":[]}""");

        await dataSource.GetModelsAsync(474, 2015, null);

        Assert.Equal(
            "https://vpic.test/api/vehicles/GetModelsForMakeIdYear/makeId/474/modelyear/2015?format=json",
            handler.LastRequestUrl);
    }

    [Fact]
    public async Task GetModelsAsync_WithVehicleType_AddsItToTheUrl()
    {
        var (dataSource, handler) = CreateDataSource("""{"Results":[]}""");

        await dataSource.GetModelsAsync(474, 2015, "Passenger Car");

        Assert.Equal(
            "https://vpic.test/api/vehicles/GetModelsForMakeIdYear/makeId/474/modelyear/2015/vehicletype/Passenger%20Car?format=json",
            handler.LastRequestUrl);
    }

    [Fact]
    public async Task ServerError_ThrowsExternalServiceException()
    {
        var (dataSource, _) = CreateDataSource("error", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<ExternalServiceException>(() => dataSource.GetMakesAsync());
    }

    [Fact]
    public async Task HtmlInsteadOfJson_ThrowsExternalServiceException()
    {
        var (dataSource, _) = CreateDataSource("<html>Service Unavailable</html>");

        await Assert.ThrowsAsync<ExternalServiceException>(() => dataSource.GetMakesAsync());
    }

    private static (VpicVehicleDataSource DataSource, FakeHttpHandler Handler) CreateDataSource(
        string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpHandler(responseBody, statusCode);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://vpic.test/api/vehicles/") };

        return (new VpicVehicleDataSource(httpClient), handler);
    }

    // Returns a fixed response and remembers the URL that was requested.
    private class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpHandler(string responseBody, HttpStatusCode statusCode)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        public string? LastRequestUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUrl = request.RequestUri?.AbsoluteUri;

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
