namespace VehicleExplorer.Domain.Rules;

public static class ModelYears
{
    // 1981 is when the standard 17-character VIN was introduced.
    public const int Min = 1981;

    // Next year's models are already on sale during the current year.
    public static int Max => DateTime.UtcNow.Year + 1;

    public static bool IsValid(int year) => year >= Min && year <= Max;

    // Newest first, for the year dropdown.
    public static List<int> All() => Enumerable.Range(Min, Max - Min + 1).Reverse().ToList();
}
