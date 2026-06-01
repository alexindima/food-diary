namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAiTokenLimitUpdate(
    long? InputLimit = null,
    long? OutputLimit = null);
