namespace FoodDiary.Web.Api.Options;

public sealed class ImageObjectDeletionOutboxOptions {
    public const string SectionName = "ImageObjectDeletionOutbox";

    public bool Enabled { get; init; } = true;
    public int BatchSize { get; init; } = 25;
    public int PollIntervalSeconds { get; init; } = 60;

    public static bool HasValidConfiguration(ImageObjectDeletionOutboxOptions options) =>
        !options.Enabled || options is { BatchSize: > 0, PollIntervalSeconds: > 0 };
}
