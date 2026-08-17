namespace FoodDiary.MailInbox.Infrastructure.Options;

public sealed class MailInboxStorageOptions {
    public const string SectionName = "MailInboxStorage";

    public int MaxMessagesPerDay { get; init; } = 5_000;

    public long MaxRawBytesPerDay { get; init; } = 512L * 1024 * 1024;

    public TimeSpan DeduplicationWindow { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan ContentRetention { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan MetadataRetention { get; init; } = TimeSpan.FromDays(365);

    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(6);

    public int CleanupBatchSize { get; init; } = 500;

    public static bool HasValidConfiguration(MailInboxStorageOptions options) =>
        options.MaxMessagesPerDay > 0 &&
        options.MaxRawBytesPerDay > 0 &&
        options.DeduplicationWindow > TimeSpan.Zero &&
        options.ContentRetention > TimeSpan.Zero &&
        options.MetadataRetention >= options.ContentRetention &&
        options.CleanupInterval > TimeSpan.Zero &&
        options.CleanupBatchSize > 0;
}
