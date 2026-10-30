namespace VehicleExplorer.Domain.Rules;

/// <summary>
/// The range of model years the application supports.
/// </summary>
public static class ModelYears
{
    /// <summary>
    /// 1981 is when the standard 17-character VIN was introduced, so vPIC data is consistent from then on.
    /// </summary>
    public const int Min = 1981;

    /// <summary>
    /// Manufacturers sell next year's models during the current calendar year.
    /// </summary>
    public static int Max(DateTimeOffset now) => now.Year + 1;

    public static bool IsValid(int year, DateTimeOffset now) => year >= Min && year <= Max(now);

    /// <summary>
    /// All supported years, newest first.
    /// </summary>
    public static IReadOnlyList<int> All(DateTimeOffset now) =>
        Enumerable.Range(Min, Max(now) - Min + 1).Reverse().ToList();
}
