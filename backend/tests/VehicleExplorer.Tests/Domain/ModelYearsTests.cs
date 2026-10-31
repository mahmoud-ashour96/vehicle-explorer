using VehicleExplorer.Domain.Rules;

namespace VehicleExplorer.Tests.Domain;

public class ModelYearsTests
{
    private static readonly int NextYear = DateTime.UtcNow.Year + 1;

    [Fact]
    public void IsValid_AcceptsYearsFrom1981ToNextYear()
    {
        Assert.True(ModelYears.IsValid(1981));
        Assert.True(ModelYears.IsValid(NextYear));

        Assert.False(ModelYears.IsValid(1980));
        Assert.False(ModelYears.IsValid(NextYear + 1));
    }

    [Fact]
    public void All_ReturnsEveryValidYearNewestFirst()
    {
        var years = ModelYears.All();

        Assert.Equal(NextYear, years[0]);
        Assert.Equal(1981, years[^1]);
    }
}
