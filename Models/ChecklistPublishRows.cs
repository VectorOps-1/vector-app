namespace vector_app_local.Models;

public record PublishUseRow(string ScopeType, string Target, DateTime PublishedAtUtc, string PublishedBy, string? PublishNote);

public record PublishOptionRow(int Id, string Name, string Type);

public record PublishVehicleOptionRow(int Id, string Callsign, string Registration, string VehicleType, string? VehicleFunction, string? VehicleSubtype, string AreaName);

public record PublishConflictWarning(string ScopeKey, string Message);
