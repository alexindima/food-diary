namespace FoodDiary.Infrastructure.Options;

public sealed class OutboxProcessingOptions {
    public const string SectionName = "OutboxProcessing";

    internal static readonly TimeSpan MinimumSafetyMargin = TimeSpan.FromSeconds(15);

    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan DispatchTimeout { get; init; } = TimeSpan.FromMinutes(4);
    public TimeSpan FinalizationTimeout { get; init; } = TimeSpan.FromSeconds(15);

    public static bool HasValidConfiguration(OutboxProcessingOptions options) =>
        options.LeaseDuration > TimeSpan.Zero &&
        options.DispatchTimeout > TimeSpan.Zero &&
        options.FinalizationTimeout > TimeSpan.Zero &&
        options.DispatchTimeout + options.FinalizationTimeout + MinimumSafetyMargin <= options.LeaseDuration;
}
