using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Ai;

public sealed class AiUsage : Entity<Guid> {
    private const int OperationMaxLength = 32;
    private const int ModelMaxLength = 64;

    public UserId UserId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }

    private AiUsage() {
    }

    public static AiUsage Create(
        UserId userId,
        string operation,
        string model,
        int inputTokens,
        int outputTokens,
        int totalTokens) {
        EnsureUserId(userId);
        var normalizedOperation = NormalizeRequiredText(operation, OperationMaxLength, nameof(operation));
        var normalizedModel = NormalizeRequiredText(model, ModelMaxLength, nameof(model));
        var normalizedInputTokens = NormalizeNonNegative(inputTokens, nameof(inputTokens));
        var normalizedOutputTokens = NormalizeNonNegative(outputTokens, nameof(outputTokens));
        var normalizedTotalTokens = NormalizeNonNegative(totalTokens, nameof(totalTokens));
        EnsureTotalTokensConsistency(normalizedInputTokens, normalizedOutputTokens, normalizedTotalTokens);

        var usage = new AiUsage {
            Id = Guid.NewGuid(),
            UserId = userId,
            Operation = normalizedOperation,
            Model = normalizedModel,
            InputTokens = normalizedInputTokens,
            OutputTokens = normalizedOutputTokens,
            TotalTokens = normalizedTotalTokens
        };
        usage.SetCreated();
        return usage;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static string NormalizeRequiredText(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static int NormalizeNonNegative(int value, string paramName) {
        return value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.")
            : value;
    }

    private static void EnsureTotalTokensConsistency(int inputTokens, int outputTokens, int totalTokens) {
        var minimalTotal = (long)inputTokens + outputTokens;
        if (totalTokens < minimalTotal) {
            throw new ArgumentOutOfRangeException(
                nameof(totalTokens),
                "TotalTokens must be greater than or equal to InputTokens + OutputTokens.");
        }
    }
}
