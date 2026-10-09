using FoodDiary.Modules.Ai.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using System.Globalization;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Ai.Domain.Entities;

public sealed class AiUsage : Entity<AiUsageId> {
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
        string normalizedOperation = NormalizeRequiredText(operation, OperationMaxLength, nameof(operation));
        string normalizedModel = NormalizeRequiredText(model, ModelMaxLength, nameof(model));
        var tokens = AiTokenUsage.FromCounts(inputTokens, outputTokens, totalTokens);
        return CreateCore(userId, normalizedOperation, normalizedModel, tokens);
    }

    public static AiUsage CreateWithTokens(UserId userId, string operation, string model, AiTokenUsage tokens) {
        EnsureUserId(userId);
        string normalizedOperation = NormalizeRequiredText(operation, OperationMaxLength, nameof(operation));
        string normalizedModel = NormalizeRequiredText(model, ModelMaxLength, nameof(model));
        ArgumentNullException.ThrowIfNull(tokens);
        return CreateCore(userId, normalizedOperation, normalizedModel, tokens);
    }

    private static AiUsage CreateCore(UserId userId, string operation, string model, AiTokenUsage tokens) {
        var usage = new AiUsage {
            Id = AiUsageId.New(),
            UserId = userId,
            Operation = operation,
            Model = model,
            InputTokens = tokens.InputTokens,
            OutputTokens = tokens.OutputTokens,
            TotalTokens = tokens.TotalTokens,
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

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

}
