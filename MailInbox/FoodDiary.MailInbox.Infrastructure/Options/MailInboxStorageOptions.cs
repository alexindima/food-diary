namespace FoodDiary.MailInbox.Infrastructure.Options;

public sealed class MailInboxStorageOptions {
    public const string SectionName = "MailInboxStorage";

    public int MaxMessagesPerDay { get; init; } = 5_000;

    public long MaxRawBytesPerDay { get; init; } = 512L * 1024 * 1024;

    public int MaxUntrustedMessagesPerDay { get; init; }

    public long MaxUntrustedRawBytesPerDay { get; init; }

    public TimeSpan DeduplicationWindow { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan ContentRetention { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan MetadataRetention { get; init; } = TimeSpan.FromDays(365);

    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(6);

    public int CleanupBatchSize { get; init; } = 500;

    public int MaxConcurrentMessageDetailReads { get; init; } = 2;

    public static bool HasValidConfiguration(MailInboxStorageOptions options) =>
        options.MaxMessagesPerDay > 0 &&
        options.MaxRawBytesPerDay > 0 &&
        (options.MaxUntrustedMessagesPerDay == 0 ||
         (options.MaxUntrustedMessagesPerDay > 0 &&
          options.MaxUntrustedMessagesPerDay < options.MaxMessagesPerDay)) &&
        (options.MaxUntrustedRawBytesPerDay == 0 ||
         (options.MaxUntrustedRawBytesPerDay > 0 &&
          options.MaxUntrustedRawBytesPerDay < options.MaxRawBytesPerDay)) &&
        options.DeduplicationWindow > TimeSpan.Zero &&
        options.ContentRetention > TimeSpan.Zero &&
        options.MetadataRetention >= options.ContentRetention &&
        options.CleanupInterval > TimeSpan.Zero &&
        options.CleanupBatchSize > 0 &&
        options.MaxConcurrentMessageDetailReads > 0;

    public int GetMaxUntrustedMessagesPerDay() =>
        MaxUntrustedMessagesPerDay > 0
            ? MaxUntrustedMessagesPerDay
            : Math.Max(1, MaxMessagesPerDay * 4 / 5);

    public long GetMaxUntrustedRawBytesPerDay() =>
        MaxUntrustedRawBytesPerDay > 0
            ? MaxUntrustedRawBytesPerDay
            : Math.Max(1, MaxRawBytesPerDay * 3 / 4);
}
