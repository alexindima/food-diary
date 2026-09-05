namespace FoodDiary.Presentation.Api.Features.Auth.Responses;

public sealed record ActiveSessionHttpResponse(
    Guid Id,
    bool IsCurrent,
    string? AuthProvider,
    string? Browser,
    string? OperatingSystem,
    string? DeviceType,
    DateTime CreatedAtUtc,
    DateTime LastActiveAtUtc);
