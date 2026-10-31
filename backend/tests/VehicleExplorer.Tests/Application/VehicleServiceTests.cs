using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Application.Services;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Application;

public class VehicleServiceTests
{
    private readonly FakeVehicleDataSource _dataSource = new();
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _service = new VehicleService(_dataSource, new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task GetMakesAsync_ReturnsMakesSortedByName()
    {
        var makes = await _service.GetMakesAsync();

        Assert.Equal(["HONDA", "TOYOTA"], makes.Select(m => m.Name));
    }

    [Fact]
    public async Task GetMakesAsync_SecondCallUsesTheCache()
    {
        await _service.GetMakesAsync();
        await _service.GetMakesAsync();

        Assert.Equal(1, _dataSource.GetMakesCallCount);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModelsSortedByName()
    {
        var models = await _service.GetModelsAsync(474, 2015, "Passenger Car");

        Assert.Equal(["Accord", "Civic"], models.Select(m => m.Name));
        Assert.Equal((474, 2015, "Passenger Car"), _dataSource.LastModelsRequest);
    }

    [Theory]
    [InlineData(1980)]
    [InlineData(3000)]
    public async Task GetModelsAsync_InvalidYear_ThrowsValidationException(int year)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.GetModelsAsync(474, year, null));

        Assert.Contains("year", exception.Errors.Keys);
        Assert.Null(_dataSource.LastModelsRequest);
    }
}
