using VehicleExplorer.Api.Models;

namespace VehicleExplorer.Api.Tests;

public sealed class ModelYearsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1980, false)]
    [InlineData(1981, true)]
    [InlineData(2026, true)]
    [InlineData(2027, true)] // next year's models are already on sale
    [InlineData(2028, false)]
    public void IsValid_AcceptsFrom1981ToNextYear(int year, bool expected) =>
        Assert.Equal(expected, ModelYears.IsValid(year, Now));

    [Fact]
    public void All_ReturnsEveryValidYearNewestFirst()
    {
        var years = ModelYears.All(Now);

        Assert.Equal(2027, years[0]);
        Assert.Equal(1981, years[^1]);
        Assert.Equal(2027 - 1981 + 1, years.Count);
    }
}
