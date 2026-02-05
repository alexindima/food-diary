using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

public sealed class AiUsage : Entity<Guid>
{
    public UserId UserId { get; private set; } = default!;
    public string Operation { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }

    private AiUsage()
    {
    }

    public static AiUsage Create(
        UserId userId,
        string operation,
        string model,
        int inputTokens,
        int outputTokens,
        int totalTokens)
    {
        var usage = new AiUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Operation = operation,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = totalTokens
        };
        usage.SetCreated();
        return usage;
    }
}
