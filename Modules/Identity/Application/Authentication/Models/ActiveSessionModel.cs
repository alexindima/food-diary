namespace FoodDiary.Application.Identity.Authentication.Models;

public sealed record ActiveSessionModel(
    Guid Id,
    bool IsCurrent,
    string? AuthProvider,
    string? Browser,
    string? OperatingSystem,
    string? DeviceType,
    DateTime CreatedAtUtc,
    DateTime LastActiveAtUtc);
