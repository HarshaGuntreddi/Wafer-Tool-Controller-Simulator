using Shared.Domain;

namespace ControllerService.Api;

public record StatusDto(bool Connected, DateTime? LastHeartbeatUtc, ToolState State);

public record TelemetryDto(DateTime Timestamp, string Key, double Value);
