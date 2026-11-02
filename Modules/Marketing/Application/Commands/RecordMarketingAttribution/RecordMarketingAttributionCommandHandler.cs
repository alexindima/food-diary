using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using System.Globalization;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Application.Common;
using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;

namespace FoodDiary.Modules.Marketing.Application.Commands.RecordMarketingAttribution;

public sealed class RecordMarketingAttributionCommandHandler(
    IMarketingAttributionEventRepository repository,
    TimeProvider timeProvider,
    IUserMarketingConsentRecorder consentRecorder)
    : IRequestHandler<RecordMarketingAttributionCommand, Result> {
    private const int EventTypeMaxLength = 32;
    private const int AnonymousIdMaxLength = 96;
    private const int SessionIdMaxLength = 96;
    private const int ReferrerHostMaxLength = 128;
    private const int UtmValueMaxLength = 160;
    private const int BuildVersionMaxLength = 64;
    private static readonly TimeSpan MaximumPastTimestampAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan MaximumFutureTimestampSkew = TimeSpan.FromMinutes(5);

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
            (request.UserId is not { } userId || userId == UserId.Empty)) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(request.UserId),
                "Signup attribution requires an authenticated user identity."));
        }

        Result<DateTime> timestampResult = ParseTimestampUtc(request.Timestamp);
        if (timestampResult.IsFailure) {
            return Result.Failure(timestampResult.Error);
        }

        if (!HasCurrentOptIn(request, timestampResult.Value)) {
            return Result.Failure(Errors.Validation.Invalid(nameof(request.ConsentNoticeVersion), "A current, prior marketing opt-in is required."));
        }
        MarketingAttributionEventRecord? trustedLanding = null;
        if (string.Equals(request.EventType, MarketingAttributionEventTypes.SignupCompleted, StringComparison.Ordinal)) {
            if (await repository.ExistsForUserAsync(request.UserId!.Value, MarketingAttributionEventTypes.SignupCompleted, cancellationToken)) {
                return Result.Success();
            }

            trustedLanding = await repository.GetLandingAsync(
                new AnonymousVisitorId(NormalizeRequired(request.AnonymousId.Value, AnonymousIdMaxLength, "unknown")),
                new MarketingSessionId(NormalizeRequired(request.SessionId.Value, SessionIdMaxLength, "unknown")),
                timeProvider.GetUtcNow().UtcDateTime - MaximumPastTimestampAge,
                cancellationToken);
            if (trustedLanding is null) {
                return Result.Success();
            }
        }

        var record = new MarketingAttributionEventRecord(
            NormalizeRequired(request.EventType, EventTypeMaxLength, "page_landing"),
            timestampResult.Value,
            request.UserId,
            trustedLanding?.AnonymousId ?? new AnonymousVisitorId(NormalizeRequired(request.AnonymousId.Value, AnonymousIdMaxLength, "unknown")),
            trustedLanding?.SessionId ?? new MarketingSessionId(NormalizeRequired(request.SessionId.Value, SessionIdMaxLength, "unknown")),
            MarketingLandingPath.Sanitize(trustedLanding?.LandingPath ?? request.LandingPath),
            trustedLanding?.ReferrerHost ?? NormalizeOptional(request.ReferrerHost, ReferrerHostMaxLength),
            trustedLanding?.UtmSource ?? NormalizeOptional(request.UtmSource, UtmValueMaxLength),
            trustedLanding?.UtmMedium ?? NormalizeOptional(request.UtmMedium, UtmValueMaxLength),
            trustedLanding?.UtmCampaign ?? NormalizeOptional(request.UtmCampaign, UtmValueMaxLength),
            trustedLanding?.UtmContent ?? NormalizeOptional(request.UtmContent, UtmValueMaxLength),
            trustedLanding?.UtmTerm ?? NormalizeOptional(request.UtmTerm, UtmValueMaxLength),
            trustedLanding?.BuildVersion ?? NormalizeOptional(request.BuildVersion, BuildVersionMaxLength),
            eventId, request.ConsentNoticeVersion, request.ConsentGrantedAtUtc);

        await repository.AddAsync(record, cancellationToken);
        if (request.UserId is { } consentingUser) {
            Result consent = await consentRecorder.RecordOptInAsync(consentingUser, PrivacyNoticeVersions.Current, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
            if (consent.IsFailure) { return consent; }
        }
        return Result.Success();
    }

    private static bool HasCurrentOptIn(RecordMarketingAttributionCommand request, DateTime occurredAtUtc) =>
        string.Equals(request.ConsentNoticeVersion, PrivacyNoticeVersions.Current, StringComparison.Ordinal)
        && request.ConsentGrantedAtUtc is { Kind: DateTimeKind.Utc } grantedAtUtc
        && grantedAtUtc <= occurredAtUtc && occurredAtUtc - grantedAtUtc < TimeSpan.FromDays(180);

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
