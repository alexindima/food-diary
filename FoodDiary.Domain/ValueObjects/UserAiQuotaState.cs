namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAiQuotaState(
    long AiInputTokenLimit,
    long AiOutputTokenLimit) {
    public static UserAiQuotaState CreateInitial(long defaultAiInputTokenLimit, long defaultAiOutputTokenLimit) {
        if (defaultAiInputTokenLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(defaultAiInputTokenLimit), "Input limit must be non-negative.");
        }

        if (defaultAiOutputTokenLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(defaultAiOutputTokenLimit), "Output limit must be non-negative.");
        }

        return new UserAiQuotaState(
            AiInputTokenLimit: defaultAiInputTokenLimit,
            AiOutputTokenLimit: defaultAiOutputTokenLimit);
    }

    public UserAiQuotaState WithLimits(long? inputLimit, long? outputLimit) {
        if (inputLimit.HasValue && inputLimit.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(inputLimit), "Input limit must be non-negative.");
        }

        if (outputLimit.HasValue && outputLimit.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(outputLimit), "Output limit must be non-negative.");
        }

        return this with {
            AiInputTokenLimit = inputLimit ?? AiInputTokenLimit,
            AiOutputTokenLimit = outputLimit ?? AiOutputTokenLimit
        };
    }
}
