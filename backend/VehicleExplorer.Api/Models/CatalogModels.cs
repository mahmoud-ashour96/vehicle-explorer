namespace VehicleExplorer.Api.Models;

// Public API contracts. Deliberately decoupled from vPIC's response shape (Make_ID, Model_Name, ...)
// so the frontend never depends on the upstream naming.

public sealed record Make(int Id, string Name);

public sealed record VehicleType(int Id, string Name);

public sealed record VehicleModel(int Id, string Name);
