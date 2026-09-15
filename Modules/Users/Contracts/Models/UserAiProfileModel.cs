using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserAiProfileModel(
    UserId UserId,
    string? Language,
    long InputTokenLimit,
    long OutputTokenLimit,
    bool HasAcceptedAiConsent);
