using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FluentValidation.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Statistics;

[ExcludeFromCodeCoverage]
public class StatisticsFeatureTests {
    [Fact]
    public async Task GetStatisticsQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetStatisticsQueryValidator();
        var query = new GetStatisticsQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var user = User.Create("statistics-invalid-date-range@example.com", "hash");
        var handler = new GetStatisticsQueryHandler(new StaticStatisticsReadService([]), CreateCurrentUserAccessService(user));
        var query = new GetStatisticsQuery(user.Id.Value, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetStatisticsQueryHandler(new StaticStatisticsReadService([]), CreateCurrentUserAccessService(user: null));
        var query = new GetStatisticsQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithEmptyMeals_ReturnsSingleZeroBucket() {
        var user = User.Create("statistics-empty@example.com", "hash");
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var handler = new GetStatisticsQueryHandler(
            new StaticStatisticsReadService([new DashboardStatisticsBucketReadModel(from, to, 0, 0, 0, 0, 0)]),
            CreateCurrentUserAccessService(user));
        var query = new GetStatisticsQuery(user.Id.Value, from, to, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Success(result);
        AggregatedStatisticsModel bucket = Assert.Single(result.Value);
        Assert.Equal(0, bucket.TotalCalories);
        Assert.Equal(0, bucket.TotalProteins);
        Assert.Equal(0, bucket.TotalFats);
        Assert.Equal(0, bucket.TotalCarbs);
        Assert.Equal(0, bucket.TotalFiber);
        Assert.Equal(from, bucket.DateFrom);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("statistics-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetStatisticsQueryHandler(new StaticStatisticsReadService([]), CreateCurrentUserAccessService(user));
        var query = new GetStatisticsQuery(user.Id.Value, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithMultiDayBucket_ReturnsTotalsAndDailyAverages() {
        var user = User.Create("statistics-multiday@example.com", "hash");
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 2, 23, 59, 59, DateTimeKind.Utc);
        var handler = new GetStatisticsQueryHandler(
            new StaticStatisticsReadService([new DashboardStatisticsBucketReadModel(
                from,
                to,
                TotalCalories: 1000,
                AverageProteins: 50,
                AverageFats: 25,
                AverageCarbs: 100,
                AverageFiber: 10,
                TotalProteins: 100,
                TotalFats: 50,
                TotalCarbs: 200,
                TotalFiber: 20)]),
            CreateCurrentUserAccessService(user));
        var query = new GetStatisticsQuery(user.Id.Value, from, to, 2);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Success(result);
        AggregatedStatisticsModel bucket = Assert.Single(result.Value);
        Assert.Equal(1000, bucket.TotalCalories);
        Assert.Equal(100, bucket.TotalProteins);
        Assert.Equal(50, bucket.TotalFats);
        Assert.Equal(200, bucket.TotalCarbs);
        Assert.Equal(20, bucket.TotalFiber);
        Assert.Equal(50, bucket.AverageProteins);
        Assert.Equal(25, bucket.AverageFats);
        Assert.Equal(100, bucket.AverageCarbs);
        Assert.Equal(10, bucket.AverageFiber);
    }

    [Fact]
    public async Task GetStatisticsQueryHandler_WithLocalDayUtcBoundaries_GroupsMealsByRequestedBoundary() {
        var user = User.Create("statistics-boundaries@example.com", "hash");
        DateTime localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        DateTime localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        var handler = new GetStatisticsQueryHandler(
            new StaticStatisticsReadService([new DashboardStatisticsBucketReadModel(
                localDayStartUtc,
                localDayEndUtc,
                TotalCalories: 946,
                AverageProteins: 59,
                AverageFats: 45,
                AverageCarbs: 76,
                AverageFiber: 7,
                TotalProteins: 59,
                TotalFats: 45,
                TotalCarbs: 76,
                TotalFiber: 7)]),
            CreateCurrentUserAccessService(user));
        var query = new GetStatisticsQuery(user.Id.Value, localDayStartUtc, localDayEndUtc, 1);

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Success(result);
        AggregatedStatisticsModel bucket = Assert.Single(result.Value);
        Assert.Equal(localDayStartUtc, bucket.DateFrom);
        Assert.Equal(localDayEndUtc, bucket.DateTo);
        Assert.Equal(946, bucket.TotalCalories);
        Assert.Equal(59, bucket.AverageProteins);
        Assert.Equal(45, bucket.AverageFats);
        Assert.Equal(76, bucket.AverageCarbs);
        Assert.Equal(7, bucket.AverageFiber);
        Assert.Equal(59, bucket.TotalProteins);
        Assert.Equal(45, bucket.TotalFats);
        Assert.Equal(76, bucket.TotalCarbs);
        Assert.Equal(7, bucket.TotalFiber);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StaticStatisticsReadService(IReadOnlyList<DashboardStatisticsBucketReadModel> buckets) : IDashboardStatisticsReadService {
        public Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> GetStatisticsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int quantizationDays,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(buckets));
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                Error? error = user switch {
                    null => Errors.Authentication.InvalidToken,
                    { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                    { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                    _ => null,
                };
                return Task.FromResult(error);
            });

        return service;
    }
}
