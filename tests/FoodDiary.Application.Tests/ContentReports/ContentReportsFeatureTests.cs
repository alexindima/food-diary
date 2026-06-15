using FoodDiary.Application.ContentReports.Commands.CreateContentReport;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.ContentReports.Models;

namespace FoodDiary.Application.Tests.ContentReports;

[ExcludeFromCodeCoverage]
public class ContentReportsFeatureTests {
    [Fact]
    public async Task CreateContentReport_WithValidData_Succeeds() {
        IContentReportRepository repository = CreateContentReportRepository();
        var handler = new CreateContentReportCommandHandler(repository);

        Result<ContentReportModel> result = await handler.Handle(
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
        IContentReportRepository repository = CreateContentReportRepository((new UserId(userId), ReportTargetType.Recipe, targetId));

        var handler = new CreateContentReportCommandHandler(repository);
        Result<ContentReportModel> result = await handler.Handle(
            new CreateContentReportCommand(userId, "Recipe", targetId, "Spam"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyReported", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateContentReport_WithNullUserId_ReturnsFailure() {
        var handler = new CreateContentReportCommandHandler(CreateContentReportRepository());

        Result<ContentReportModel> result = await handler.Handle(
            new CreateContentReportCommand(UserId: null, "Recipe", Guid.NewGuid(), "Spam"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static IContentReportRepository CreateContentReportRepository(
        params (UserId UserId, ReportTargetType TargetType, Guid TargetId)[] reported) {
        HashSet<(UserId UserId, ReportTargetType TargetType, Guid TargetId)> reportedSet = [.. reported];
        IContentReportRepository repository = Substitute.For<IContentReportRepository>();
        repository
            .AddAsync(Arg.Any<ContentReport>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<ContentReport>(0)));
        repository
            .HasUserReportedAsync(Arg.Any<UserId>(), Arg.Any<ReportTargetType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.ArgAt<UserId>(0);
                ReportTargetType targetType = call.ArgAt<ReportTargetType>(1);
                Guid targetId = call.ArgAt<Guid>(2);
                return Task.FromResult(reportedSet.Contains((userId, targetType, targetId)));
            });

        return repository;
    }
}
