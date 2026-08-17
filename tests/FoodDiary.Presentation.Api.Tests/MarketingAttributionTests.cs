using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;
using FoodDiary.Application.Marketing.Services;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Features.Marketing;
using FoodDiary.Presentation.Api.Features.Marketing.Requests;
using FoodDiary.Mediator;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionTests {
    [Fact]
    public void AnonymousRequestValidation_AcceptsValuesAtEveryBoundary() {
        var request = new MarketingAttributionHttpRequest(
            Timestamp: new string('t', 64),
            AnonymousId: new string('a', 96),
            SessionId: new string('s', 96),
            LandingPath: new string('p', 512),
            ReferrerHost: new string('r', 128),
            UtmSource: new string('u', 160),
            UtmMedium: new string('m', 160),
            UtmCampaign: new string('c', 160),
            UtmContent: new string('o', 160),
            UtmTerm: new string('e', 160),
            BuildVersion: new string('b', 64));

        IReadOnlyList<ValidationResult> errors = Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void AnonymousRequestValidation_RejectsMissingOversizedAndControlCharacterValues() {
        var request = new MarketingAttributionHttpRequest(
            Timestamp: " ",
            AnonymousId: new string('a', 97),
            SessionId: "session\n",
            LandingPath: null!,
            ReferrerHost: new string('r', 129),
            UtmSource: "source\t",
            UtmMedium: null,
            UtmCampaign: new string('c', 161) + "\n",
            UtmContent: new string('o', 160),
            UtmTerm: string.Empty,
            BuildVersion: new string('b', 65));

        IReadOnlyList<ValidationResult> errors = Validate(request);

        Assert.Multiple(
            () => AssertValidationError(errors, "Timestamp", "Value is required."),
            () => AssertValidationError(errors, "AnonymousId", "Value must be at most 96 characters."),
            () => AssertValidationError(errors, "SessionId", "Control characters are not supported."),
            () => AssertValidationError(errors, "LandingPath", "Value is required."),
            () => AssertValidationError(errors, "ReferrerHost", "Value must be at most 128 characters."),
            () => AssertValidationError(errors, "UtmSource", "Control characters are not supported."),
            () => AssertValidationError(errors, "UtmCampaign", "Value must be at most 160 characters."),
            () => AssertValidationError(errors, "UtmCampaign", "Control characters are not supported."),
            () => AssertValidationError(errors, "BuildVersion", "Value must be at most 64 characters."));
    }

    [Fact]
    public void SignupRequestValidation_UsesTheSamePublicInputContract() {
        var request = new MarketingSignupAttributionHttpRequest(
            Timestamp: "2026-08-18T12:00:00Z",
            AnonymousId: "anonymous",
            SessionId: "session",
            LandingPath: "/",
            BuildVersion: "build\r");

        IReadOnlyList<ValidationResult> errors = Validate(request);

        ValidationResult error = Assert.Single(errors);
        Assert.Multiple(
            () => Assert.Equal("Control characters are not supported.", error.ErrorMessage),
            () => Assert.Equal(["BuildVersion"], error.MemberNames, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Create_MapsMarketingAttributionRequestToCommand() {
        IRequest<Result>? sentRequest = null;
        var controller = new MarketingAttributionController(SubstituteSender.Create(Result.Success(), request => sentRequest = request)) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        var request = new MarketingAttributionHttpRequest(
            Timestamp: DateTime.UtcNow.ToString("O"),
            AnonymousId: "fd-anon-test",
            SessionId: "fd-session-test",
            LandingPath: "/?utm_source=telegram",
            ReferrerHost: "t.me",
            UtmSource: "telegram",
            UtmMedium: "social",
            UtmCampaign: "launch",
            BuildVersion: "test");

        var eventId = Guid.NewGuid();
        IActionResult result = await controller.Create(eventId, request);

        Assert.IsType<NoContentResult>(result);
        RecordMarketingAttributionCommand command = Assert.IsType<RecordMarketingAttributionCommand>(sentRequest);
        Assert.Equal("fd-anon-test", command.AnonymousId);
        Assert.Null(command.UserId);
        Assert.Equal("page_landing", command.EventType);
        Assert.Equal("telegram", command.UtmSource);
        Assert.Equal("launch", command.UtmCampaign);
        Assert.Equal(eventId, command.EventId);
    }

    [Fact]
    public async Task CreateSignup_UsesOnlyCurrentUserIdentity() {
        IRequest<Result>? sentRequest = null;
        var controller = new MarketingAttributionController(SubstituteSender.Create(Result.Success(), request => sentRequest = request)) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        var userId = Guid.NewGuid();
        var request = new MarketingSignupAttributionHttpRequest(
            Timestamp: DateTime.UtcNow.ToString("O"),
            AnonymousId: "fd-anon-test",
            SessionId: "fd-session-test",
            LandingPath: "/?utm_source=telegram",
            UtmSource: "telegram");

        var eventId = Guid.NewGuid();
        IActionResult result = await controller.CreateSignup(userId, eventId, request);

        Assert.IsType<NoContentResult>(result);
        RecordMarketingAttributionCommand command = Assert.IsType<RecordMarketingAttributionCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("signup_completed", command.EventType);
        Assert.Equal(eventId, command.EventId);
    }

    [Fact]
    public void AnonymousRequestContract_DoesNotExposeIdentityOrEventType() {
        string[] forbiddenProperties = ["UserId", "EventType"];

        Assert.DoesNotContain(
            typeof(MarketingAttributionHttpRequest).GetProperties(),
            property => forbiddenProperties.Contains(property.Name, StringComparer.Ordinal));
    }

    [Fact]
    public async Task AdminGetSummary_MapsQueryToMarketingAttributionSummary() {
        var summary = new MarketingAttributionSummaryModel(
            WindowHours: 720,
            GeneratedAtUtc: DateTime.UtcNow,
            Events: 1,
            Visits: 1,
            Signups: 0,
            PremiumStarts: 0,
            AnonymousVisitors: 1,
            Sessions: 1,
            AttributedEvents: 1,
            OrganicEvents: 0,
            AttributedVisits: 1,
            OrganicVisits: 0,
            SignupRatePercent: 0,
            PremiumRatePercent: 0,
            LastEventAtUtc: DateTime.UtcNow,
            TopCampaigns: [
                new MarketingAttributionBreakdownModel(
                    "telegram",
                    "social",
                    "launch",
                    Events: 1,
                    Visits: 1,
                    Signups: 0,
                    PremiumStarts: 0,
                    AnonymousVisitors: 1,
                    Sessions: 1,
                    SignupRatePercent: 0,
                    PremiumRatePercent: 0,
                    LastEventAtUtc: DateTime.UtcNow),
            ],
            TopSources: [
                new MarketingAttributionBreakdownModel(
                    "telegram",
                    "social",
                    "all",
                    Events: 1,
                    Visits: 1,
                    Signups: 0,
                    PremiumStarts: 0,
                    AnonymousVisitors: 1,
                    Sessions: 1,
                    SignupRatePercent: 0,
                    PremiumRatePercent: 0,
                    LastEventAtUtc: DateTime.UtcNow),
            ],
            RecentEvents: [
                new MarketingAttributionRecentEventModel(
                    OccurredAtUtc: DateTime.UtcNow,
                    EventType: "page_landing",
                    AnonymousId: "fd-anon-test",
                    SessionId: "fd-session-test",
                    LandingPath: "/",
                    ReferrerHost: "t.me",
                    UtmSource: "telegram",
                    UtmMedium: "social",
                    UtmCampaign: "launch",
                    UtmContent: null,
                    UtmTerm: null,
                    BuildVersion: "test"),
            ]);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(summary));
        var controller = new AdminAcquisitionController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

        IActionResult result = await controller.GetSummary(new GetMarketingAttributionSummaryHttpQuery(720));

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        MarketingAttributionSummaryHttpResponse response = Assert.IsType<MarketingAttributionSummaryHttpResponse>(ok.Value);
        Assert.IsType<GetMarketingAttributionSummaryQuery>(sender.Request);
        Assert.Equal(1, response.Events);
        Assert.Single(response.TopCampaigns);
        Assert.Single(response.RecentEvents);
    }

    [Fact]
    public async Task GetSummaryAsync_AggregatesAttributedAndOrganicEvents() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        DateTime now = new(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
        var handler = new GetMarketingAttributionSummaryQueryHandler(new MarketingAttributionSummaryReadService(repository, new FixedTimeProvider(now)));
        await repository.AddAsync(new MarketingAttributionEventRecord(
            EventType: "page_landing",
            OccurredAtUtc: now.AddHours(-1),
            UserId: null,
            AnonymousId: "anon-1",
            SessionId: "session-1",
            LandingPath: "/?utm_source=telegram&utm_medium=social&utm_campaign=launch",
            ReferrerHost: "t.me",
            UtmSource: "telegram",
            UtmMedium: "social",
            UtmCampaign: "launch",
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: "test"));
        await repository.AddAsync(new MarketingAttributionEventRecord(
            EventType: "page_landing",
            OccurredAtUtc: now.AddHours(-2),
            UserId: null,
            AnonymousId: "anon-2",
            SessionId: "session-2",
            LandingPath: "/",
            ReferrerHost: null,
            UtmSource: null,
            UtmMedium: null,
            UtmCampaign: null,
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: "test"));

        Result<MarketingAttributionSummaryModel> result = await handler.Handle(new GetMarketingAttributionSummaryQuery(24), CancellationToken.None);
        MarketingAttributionSummaryModel summary = result.Value;

        Assert.Equal(2, summary.Events);
        Assert.Equal(2, summary.Visits);
        Assert.Equal(0, summary.Signups);
        Assert.Equal(2, summary.AnonymousVisitors);
        Assert.Equal(2, summary.Sessions);
        Assert.Equal(1, summary.AttributedEvents);
        Assert.Equal(1, summary.OrganicEvents);
        Assert.Equal(1, summary.AttributedVisits);
        Assert.Equal(1, summary.OrganicVisits);
        MarketingAttributionBreakdownModel campaign = Assert.Single(summary.TopCampaigns);
        Assert.Equal("telegram", campaign.Source);
        Assert.Equal("social", campaign.Medium);
        Assert.Equal("launch", campaign.Campaign);
        Assert.Equal(2, summary.TopSources.Count);
        Assert.Contains(
            summary.TopSources,
            source => string.Equals(source.Source, "direct", StringComparison.Ordinal) && source.Visits == 1);
        Assert.Equal(2, summary.RecentEvents.Count);
    }

    [Fact]
    public async Task GetSummaryAsync_DoesNotTreatReferralWithoutUtmCampaignAsCampaign() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        DateTime now = new(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
        var handler = new GetMarketingAttributionSummaryQueryHandler(
            new MarketingAttributionSummaryReadService(repository, new FixedTimeProvider(now)));
        await repository.AddAsync(new MarketingAttributionEventRecord(
            EventType: "page_landing",
            OccurredAtUtc: now.AddHours(-1),
            UserId: null,
            AnonymousId: "anon-referral",
            SessionId: "session-referral",
            LandingPath: "/",
            ReferrerHost: "example.test",
            UtmSource: null,
            UtmMedium: null,
            UtmCampaign: null,
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: "test"));

        Result<MarketingAttributionSummaryModel> result =
            await handler.Handle(new GetMarketingAttributionSummaryQuery(24), CancellationToken.None);
        MarketingAttributionSummaryModel summary = result.Value;

        Assert.Empty(summary.TopCampaigns);
        MarketingAttributionBreakdownModel source = Assert.Single(summary.TopSources);
        Assert.Multiple(
            () => Assert.Equal("example.test", source.Source),
            () => Assert.Equal("referral", source.Medium),
            () => Assert.Equal(1, source.Visits));
    }

    [Fact]
    public async Task RecordAsync_TruncatesPublicAttributionValues() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, TimeProvider.System);
        string longValue = new('x', 300);

        await handler.Handle(
            new RecordMarketingAttributionCommand(
                "page_landing",
                DateTime.UtcNow.ToString("O"),
                UserId: null,
                longValue,
                longValue,
                longValue,
                longValue,
                longValue,
                longValue,
                longValue,
                longValue,
                longValue,
                longValue,
                Guid.NewGuid()),
            CancellationToken.None);

        MarketingAttributionEventRecord record = Assert.Single(repository.Events);
        Assert.Equal("page_landing", record.EventType);
        Assert.Equal(96, record.AnonymousId.Length);
        Assert.Equal(96, record.SessionId.Length);
        Assert.Equal(160, record.UtmSource?.Length);
        Assert.Equal(64, record.BuildVersion?.Length);
    }

    [Fact]
    public async Task RecordAsync_RejectsUnsupportedEventType() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, TimeProvider.System);

        Result result = await handler.Handle(
            CreateRecordCommand("premium_started", userId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Events);
    }

    [Fact]
    public async Task RecordAsync_RejectsMissingEventId() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, TimeProvider.System);

        Result result = await handler.Handle(
            CreateRecordCommand("page_landing", userId: null) with { EventId = null },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Events);
    }

    [Fact]
    public async Task RecordAsync_RejectsSignupWithoutAuthenticatedUser() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, TimeProvider.System);

        Result result = await handler.Handle(
            CreateRecordCommand("signup_completed", userId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Events);
    }

    [Fact]
    public async Task RecordAsync_RejectsTimestampOutsideIngestionWindow() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        DateTime now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        var handler = new RecordMarketingAttributionCommandHandler(repository, new FixedTimeProvider(now));
        RecordMarketingAttributionCommand command = CreateRecordCommand("page_landing", userId: null) with {
            Timestamp = now.AddDays(-2).ToString("O"),
        };

        Result result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Events);
    }

    private static RecordMarketingAttributionCommand CreateRecordCommand(string eventType, Guid? userId) =>
        new(
            eventType,
            DateTime.UtcNow.ToString("O"),
            userId,
            "anon-1",
            "session-1",
            "/",
            ReferrerHost: null,
            UtmSource: null,
            UtmMedium: null,
            UtmCampaign: null,
            UtmContent: null,
            UtmTerm: null,
            BuildVersion: null,
            EventId: Guid.NewGuid());

    private static IReadOnlyList<ValidationResult> Validate(object instance) {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }

    private static void AssertValidationError(
        IEnumerable<ValidationResult> errors,
        string memberName,
        string message) {
        Assert.Contains(errors, error =>
            string.Equals(error.ErrorMessage, message, StringComparison.Ordinal) &&
            error.MemberNames.Contains(memberName, StringComparer.Ordinal));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryMarketingAttributionEventRepository : IMarketingAttributionEventRepository {
        private readonly List<MarketingAttributionEventRecord> _events = [];

        public IReadOnlyList<MarketingAttributionEventRecord> Events => _events;

        public Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default) {
            _events.Add(record);
            return Task.CompletedTask;
        }

        public Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MarketingAttributionEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
            return Task.FromResult<IReadOnlyList<MarketingAttributionEventRecord>>(_events.Where(x => x.OccurredAtUtc >= sinceUtc).ToList());
        }

        public Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default) {
            return Task.FromResult(_events
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.OccurredAtUtc)
                .FirstOrDefault());
        }

        public Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default) {
            return Task.FromResult(_events.Any(x =>
                x.UserId == userId &&
                string.Equals(x.EventType, eventType, StringComparison.Ordinal)));
        }
    }
}
