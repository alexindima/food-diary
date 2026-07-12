using System.Globalization;
using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;

public sealed class RecordMarketingAttributionCommandHandler(
    IMarketingAttributionEventWriteRepository repository,
    TimeProvider timeProvider)
    : ICommandHandler<RecordMarketingAttributionCommand, Result> {
    private const int EventTypeMaxLength = 32;
    private const int AnonymousIdMaxLength = 96;
    private const int SessionIdMaxLength = 96;
    private const int LandingPathMaxLength = 512;
    private const int ReferrerHostMaxLength = 128;
    private const int UtmValueMaxLength = 160;
    private const int BuildVersionMaxLength = 64;

    public async Task<Result> Handle(RecordMarketingAttributionCommand command, CancellationToken cancellationToken) {
        var record = new MarketingAttributionEventRecord(
            NormalizeRequired(command.EventType, EventTypeMaxLength, "page_landing"),
            ParseTimestampUtc(command.Timestamp),
            command.UserId,
            NormalizeRequired(command.AnonymousId, AnonymousIdMaxLength, "unknown"),
            NormalizeRequired(command.SessionId, SessionIdMaxLength, "unknown"),
            NormalizeRequired(command.LandingPath, LandingPathMaxLength, "/"),
            NormalizeOptional(command.ReferrerHost, ReferrerHostMaxLength),
            NormalizeOptional(command.UtmSource, UtmValueMaxLength),
            NormalizeOptional(command.UtmMedium, UtmValueMaxLength),
            NormalizeOptional(command.UtmCampaign, UtmValueMaxLength),
            NormalizeOptional(command.UtmContent, UtmValueMaxLength),
            NormalizeOptional(command.UtmTerm, UtmValueMaxLength),
            NormalizeOptional(command.BuildVersion, BuildVersionMaxLength));

        await repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private DateTime ParseTimestampUtc(string? timestamp) {
        return DateTime.TryParse(
            timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime parsed)
            ? parsed
            : timeProvider.GetUtcNow().UtcDateTime;
    }

    private static string NormalizeRequired(string? value, int maxLength, string fallback) {
        string? normalized = NormalizeOptional(value, maxLength);
        return normalized ?? fallback;
    }

    private static string? NormalizeOptional(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
