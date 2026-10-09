namespace FoodDiary.Modules.Ai.Domain.ValueObjects;

public sealed record AiTokenUsage {
    public int InputTokens { get; }
    public int OutputTokens { get; }
    public int TotalTokens { get; }

    private AiTokenUsage(int inputTokens, int outputTokens, int totalTokens) {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = totalTokens;
    }

    public static AiTokenUsage FromCounts(int inputTokens, int outputTokens, int totalTokens) {
        EnsureNonNegative(inputTokens, nameof(inputTokens));
        EnsureNonNegative(outputTokens, nameof(outputTokens));
        EnsureNonNegative(totalTokens, nameof(totalTokens));
        if (totalTokens < (long)inputTokens + outputTokens) {
            throw new ArgumentOutOfRangeException(nameof(totalTokens),
                "TotalTokens must be greater than or equal to InputTokens + OutputTokens.");
        }

        return new AiTokenUsage(inputTokens, outputTokens, totalTokens);
    }

    public static bool TryFromProviderCounts(int inputTokens, int outputTokens, int totalTokens, out AiTokenUsage? usage) {
        if (inputTokens < 0 || outputTokens < 0 || totalTokens < (long)inputTokens + outputTokens) {
            usage = null;
            return false;
        }

        usage = new AiTokenUsage(inputTokens, outputTokens, totalTokens);
        return true;
    }

    private static void EnsureNonNegative(int value, string paramName) {
        if (value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.");
        }
    }
}
