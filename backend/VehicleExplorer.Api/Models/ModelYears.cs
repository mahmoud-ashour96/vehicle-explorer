namespace VehicleExplorer.Api.Models;

/// <summary>Single source of truth for which model years the app accepts.</summary>
public static class ModelYears
{
    // 1981 is when the standard 17-character VIN, and with it consistent vPIC data, begins.
    public const int Min = 1981;

    // Manufacturers sell next year's models during the current calendar year.
    public static int Max(DateTimeOffset now) => now.Year + 1;

    public static bool IsValid(int year, DateTimeOffset now) => year >= Min && year <= Max(now);

    /// <summary>All accepted years, newest first.</summary>
    public static IReadOnlyList<int> All(DateTimeOffset now) =>
        Enumerable.Range(Min, Max(now) - Min + 1).Reverse().ToList();
}
