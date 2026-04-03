namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAdminAiQuotaUpdate(
    long? AiInputTokenLimit = null,
    long? AiOutputTokenLimit = null) {
    public UserAiTokenLimitUpdate ToAiTokenLimitUpdate() {
        return new UserAiTokenLimitUpdate(AiInputTokenLimit, AiOutputTokenLimit);
    }
}
