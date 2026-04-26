using FoodDiary.Application.ContentReports.Commands.CreateContentReport;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.ContentReports;

public class ContentReportsFeatureTests {
    [Fact]
    public async Task CreateContentReport_WithValidData_Succeeds() {
        var repo = new InMemoryContentReportRepository();
        var handler = new CreateContentReportCommandHandler(repo);

        var result = await handler.Handle(
            new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.NewGuid(), "Spam content"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal("Spam content", result.Value.Reason);
        Assert.Equal("Recipe", result.Value.TargetType);
    }

    [Fact]
    public async Task CreateContentReport_WhenAlreadyReported_ReturnsFailure() {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var repo = new InMemoryContentReportRepository();
        repo.SeedReported(new UserId(userId), ReportTargetType.Recipe, targetId);

        var handler = new CreateContentReportCommandHandler(repo);
        var result = await handler.Handle(
            new CreateContentReportCommand(userId, "Recipe", targetId, "Spam"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyReported", result.Error.Code);
    }

    [Fact]
    public async Task CreateContentReport_WithNullUserId_ReturnsFailure() {
        var handler = new CreateContentReportCommandHandler(new InMemoryContentReportRepository());

        var result = await handler.Handle(
            new CreateContentReportCommand(null, "Recipe", Guid.NewGuid(), "Spam"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private sealed class InMemoryContentReportRepository : IContentReportRepository {
        private readonly List<ContentReport> _reports = [];
        private readonly HashSet<(UserId, ReportTargetType, Guid)> _reported = [];

        public void SeedReported(UserId userId, ReportTargetType type, Guid targetId) =>
            _reported.Add((userId, type, targetId));

        public Task<ContentReport> AddAsync(ContentReport report, CancellationToken ct = default) {
            _reports.Add(report);
            return Task.FromResult(report);
        }

        public Task<bool> HasUserReportedAsync(UserId userId, ReportTargetType targetType, Guid targetId, CancellationToken ct = default) =>
            Task.FromResult(_reported.Contains((userId, targetType, targetId)));

        public Task<ContentReport?> GetByIdAsync(ContentReportId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(ContentReport report, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<ContentReport> Items, int Total)> GetPagedAsync(ReportStatus? status, int page, int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountByStatusAsync(ReportStatus status, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
