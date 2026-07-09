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
    public async Task Create_MapsMarketingAttributionRequestToCommand() {
        IRequest<Result>? sentRequest = null;
        var controller = new MarketingAttributionController(SubstituteSender.Create(Result.Success(), request => sentRequest = request)) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        var request = new MarketingAttributionHttpRequest(
            EventType: "page_landing",
            Timestamp: DateTime.UtcNow.ToString("O"),
            UserId: null,
            AnonymousId: "fd-anon-test",
            SessionId: "fd-session-test",
            LandingPath: "/?utm_source=telegram",
            ReferrerHost: "t.me",
            UtmSource: "telegram",
            UtmMedium: "social",
            UtmCampaign: "launch",
            BuildVersion: "test");

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        RecordMarketingAttributionCommand command = Assert.IsType<RecordMarketingAttributionCommand>(sentRequest);
        Assert.Equal("fd-anon-test", command.AnonymousId);
        Assert.Equal("telegram", command.UtmSource);
        Assert.Equal("launch", command.UtmCampaign);
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
        MarketingAttributionBreakdownModel campaign = Assert.Single(summary.TopCampaigns);
        Assert.Equal("telegram", campaign.Source);
        Assert.Equal("social", campaign.Medium);
        Assert.Equal("launch", campaign.Campaign);
        Assert.Equal(2, summary.RecentEvents.Count);
    }

    [Fact]
    public async Task RecordAsync_TruncatesPublicAttributionValues() {
        var repository = new InMemoryMarketingAttributionEventRepository();
        var handler = new RecordMarketingAttributionCommandHandler(repository, TimeProvider.System);
        string longValue = new('x', 300);

        await handler.Handle(
            new RecordMarketingAttributionCommand(
                longValue,
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
                longValue),
            CancellationToken.None);

        MarketingAttributionEventRecord record = Assert.Single(repository.Events);
        Assert.Equal(32, record.EventType.Length);
        Assert.Equal(96, record.AnonymousId.Length);
        Assert.Equal(96, record.SessionId.Length);
        Assert.Equal(160, record.UtmSource?.Length);
        Assert.Equal(64, record.BuildVersion?.Length);
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
