namespace FoodDiary.Infrastructure.Options;

public sealed class DatabaseOptions {
    public const string SectionName = "Database";

    public bool EnableRetries { get; init; } = true;

    public int MaxRetryCount { get; init; } = 3;

    public int MaxRetryDelaySeconds { get; init; } = 5;
}
