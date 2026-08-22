using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;

public sealed class RecordMarketingAttributionCommandHandler(
    IMarketingAttributionEventWriteRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<RecordMarketingAttributionCommand, Result> {
    private const int EventTypeMaxLength = 32;
    private const int AnonymousIdMaxLength = 96;
    private const int SessionIdMaxLength = 96;
    private const int LandingPathMaxLength = 512;
    private const int ReferrerHostMaxLength = 128;
    private const int UtmValueMaxLength = 160;
    private const int BuildVersionMaxLength = 64;
    private static readonly TimeSpan MaximumPastTimestampAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan MaximumFutureTimestampSkew = TimeSpan.FromMinutes(5);

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<Result> Handle(RecordMarketingAttributionCommand request, CancellationToken cancellationToken) {
        if (!MarketingAttributionEventTypes.IsClientIngestionSupported(request.EventType)) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(request.EventType),
                "Unsupported marketing attribution event type."));
        }

        if (request.EventId is not { } eventId || eventId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(request.EventId),
                "Marketing attribution event ID is required."));
        }

        if (string.Equals(request.EventType, MarketingAttributionEventTypes.PageLanding, StringComparison.Ordinal) &&
            request.UserId is not null) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(request.UserId),
                "Anonymous landing attribution cannot contain a user identity."));
        }

        if (string.Equals(request.EventType, MarketingAttributionEventTypes.SignupCompleted, StringComparison.Ordinal) &&
            (request.UserId is not { } userId || userId == Guid.Empty)) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(request.UserId),
                "Signup attribution requires an authenticated user identity."));
        }

        Result<DateTime> timestampResult = ParseTimestampUtc(request.Timestamp);
        if (timestampResult.IsFailure) {
            return Result.Failure(timestampResult.Error);
        }

        var record = new MarketingAttributionEventRecord(
            NormalizeRequired(request.EventType, EventTypeMaxLength, "page_landing"),
            timestampResult.Value,
            request.UserId,
            NormalizeRequired(request.AnonymousId, AnonymousIdMaxLength, "unknown"),
            NormalizeRequired(request.SessionId, SessionIdMaxLength, "unknown"),
            NormalizeRequired(request.LandingPath, LandingPathMaxLength, "/"),
            NormalizeOptional(request.ReferrerHost, ReferrerHostMaxLength),
            NormalizeOptional(request.UtmSource, UtmValueMaxLength),
            NormalizeOptional(request.UtmMedium, UtmValueMaxLength),
            NormalizeOptional(request.UtmCampaign, UtmValueMaxLength),
            NormalizeOptional(request.UtmContent, UtmValueMaxLength),
            NormalizeOptional(request.UtmTerm, UtmValueMaxLength),
            NormalizeOptional(request.BuildVersion, BuildVersionMaxLength),
            eventId);

        await repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private Result<DateTime> ParseTimestampUtc(string? timestamp) {
        if (!DateTime.TryParse(
            timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime parsed)) {
            return Result.Failure<DateTime>(Errors.Validation.Invalid(
                nameof(RecordMarketingAttributionCommand.Timestamp),
                "Timestamp must be a valid UTC date and time."));
        }

        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (parsed < nowUtc - MaximumPastTimestampAge || parsed > nowUtc + MaximumFutureTimestampSkew) {
            return Result.Failure<DateTime>(Errors.Validation.Invalid(
                nameof(RecordMarketingAttributionCommand.Timestamp),
                "Timestamp is outside the accepted ingestion window."));
        }

        return Result.Success(parsed);
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
