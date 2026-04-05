using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Models;

public sealed record WearableTokenResult(
    string AccessToken,
    string? RefreshToken,
    string ExternalUserId,
    DateTime? ExpiresAtUtc);

public sealed record WearableDataPoint(
    WearableDataType DataType,
    double Value);

public sealed record WearableConnectionModel(
    string Provider,
    string ExternalUserId,
    bool IsActive,
    DateTime? LastSyncedAtUtc,
    DateTime ConnectedAtUtc);

public sealed record WearableDailySummaryModel(
    DateTime Date,
    double? Steps,
    double? HeartRate,
    double? CaloriesBurned,
    double? ActiveMinutes,
    double? SleepMinutes);
