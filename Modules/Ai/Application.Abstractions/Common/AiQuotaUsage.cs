using FoodDiary.Modules.Ai.Domain.ValueObjects;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record AiQuotaUsage {
    public string Operation { get; }
    public string Model { get; }
    public AiTokenUsage Tokens { get; }

    public AiQuotaUsage(string operation, string model, AiTokenUsage tokens) {
        ArgumentNullException.ThrowIfNull(tokens);
        Operation = operation;
        Model = model;
        Tokens = tokens;
    }
    public int InputTokens => Tokens.InputTokens;
    public int OutputTokens => Tokens.OutputTokens;
    public int TotalTokens => Tokens.TotalTokens;
}
