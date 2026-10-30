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
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero));
        _service = new VehicleService(_dataSource, cache, clock);
    }

    [Fact]
    public async Task GetMakesAsync_ReturnsMakesSortedByName()
    {
        var makes = await _service.GetMakesAsync();

        Assert.Equal(["HONDA", "TOYOTA"], makes.Select(m => m.Name));
        Assert.Equal([474, 448], makes.Select(m => m.Id));
    }

    [Fact]
    public async Task GetMakesAsync_SecondCallIsServedFromCache()
    {
        await _service.GetMakesAsync();
        await _service.GetMakesAsync();

        Assert.Equal(1, _dataSource.GetMakesCallCount);
    }

    [Fact]
    public async Task GetMakesAsync_FailureIsNotCached()
    {
        _dataSource.ShouldFail = true;
        await Assert.ThrowsAsync<ExternalServiceException>(() => _service.GetMakesAsync());

        _dataSource.ShouldFail = false;
        var makes = await _service.GetMakesAsync();

        Assert.Equal(2, makes.Count);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModelsSortedByName()
    {
        var models = await _service.GetModelsAsync(474, 2015, null);

        Assert.Equal(["Accord", "Civic"], models.Select(m => m.Name));
    }

    [Fact]
    public async Task GetModelsAsync_TrimsVehicleTypeBeforeCallingDataSource()
    {
        await _service.GetModelsAsync(474, 2015, "  Passenger Car ");

        Assert.Equal((474, 2015, "Passenger Car"), _dataSource.LastModelsRequest);
    }

    [Theory]
    [InlineData(1980)]
    [InlineData(2028)]
    public async Task GetModelsAsync_YearOutOfRange_ThrowsValidationException(int year)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.GetModelsAsync(474, year, null));

        Assert.Contains("year", exception.Errors.Keys);
        Assert.Null(_dataSource.LastModelsRequest);
    }

    [Theory]
    [InlineData("../../secret")]
    [InlineData("car?format=xml")]
    public async Task GetModelsAsync_UnsafeVehicleType_ThrowsValidationException(string vehicleType)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetModelsAsync(474, 2015, vehicleType));

        Assert.Contains("vehicleType", exception.Errors.Keys);
    }

    [Fact]
    public async Task GetVehicleTypesAsync_InvalidMakeId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetVehicleTypesAsync(0));
    }

    [Fact]
    public void GetModelYears_ReturnsYearsUpToNextYear()
    {
        var years = _service.GetModelYears();

        Assert.Equal(2027, years[0]);
        Assert.Equal(1981, years[^1]);
    }
}
