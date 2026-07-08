using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.Cycles.Services;
using System.Reflection;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

[ExcludeFromCodeCoverage]
public partial class CyclesFeatureTests {









































    private static CreateCycleCommand CreateCommand(Guid userId) =>
        new(
            userId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

    private static DashboardStatisticsBucketReadModel CreateNutritionBucket(DateTime date, double calories, double fiber) =>
        new(date, date, calories, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: fiber, TotalFiber: fiber);

    private static GetCurrentCycleQueryHandler CreateCurrentCycleHandler(
        ICycleReadModelRepository cycleRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new CycleReadService(cycleRepository, CreateStatisticsReadService([])), currentUserAccessService);

    private static GetCycleNutritionSummaryQueryHandler CreateCycleNutritionSummaryHandler(
        ICycleReadModelRepository cycleRepository,
        IDashboardStatisticsReadService statisticsReadService,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new CycleReadService(cycleRepository, statisticsReadService), currentUserAccessService);

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        PropertyInfo? property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopCycleRepository : ICycleRepository {
        public Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);

        public Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<CycleProfile?> GetByIdAsync(CycleProfileId id, UserId userId, bool includeDetails = false, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult<CycleProfile?>(null);

        public Task<CycleProfile?> GetCurrentAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<CycleProfile?>(null);

        public Task<CycleProfileReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CycleProfileReadModel?>(null);

        public Task<IReadOnlyList<CycleProfile>> GetByUserAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CycleProfile>>([]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryCycleRepository(CycleProfile profile) : ICycleRepository {
        public bool WasUpdated { get; private set; }

        public Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);

        public Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
            WasUpdated = true;
            return Task.CompletedTask;
        }

        public Task<CycleProfile?> GetByIdAsync(CycleProfileId id, UserId userId, bool includeDetails = false, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.Id == id && profile.UserId == userId ? profile : null);

        public Task<CycleProfile?> GetCurrentAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.UserId == userId ? profile : null);

        public Task<CycleProfileReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.UserId == userId ? ToReadModel(profile) : null);

        public Task<IReadOnlyList<CycleProfile>> GetByUserAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CycleProfile>>(profile.UserId == userId ? [profile] : []);
        private static CycleProfileReadModel ToReadModel(CycleProfile profile) =>
            new(
                profile.Id.Value,
                profile.UserId.Value,
                profile.Mode,
                profile.Confidence,
                profile.TrackingStartDate,
                profile.AverageCycleLength,
                profile.AveragePeriodLength,
                profile.LutealLength,
                profile.IsRegular,
                profile.IsOnboardingComplete,
                profile.ShowFertilityEstimates,
                profile.DiscreetNotifications,
                profile.Notes,
                [.. profile.BleedingEntries.Select(static entry => new BleedingEntryReadModel(
                    entry.Id.Value,
                    entry.CycleProfileId.Value,
                    entry.Date,
                    entry.Type,
                    entry.Flow,
                    entry.PainImpact,
                    entry.Notes))],
                [.. profile.SymptomEntries.Select(static entry => new CycleSymptomEntryReadModel(
                    entry.Id.Value,
                    entry.CycleProfileId.Value,
                    entry.Date,
                    entry.Category,
                    entry.Intensity,
                    entry.Tags,
                    entry.Note))],
                [.. profile.Factors.Select(static factor => new CycleFactorReadModel(
                    factor.Id.Value,
                    factor.CycleProfileId.Value,
                    factor.Type,
                    factor.StartDate,
                    factor.EndDate,
                    factor.Notes))],
                [.. profile.FertilitySignals.Select(static signal => new FertilitySignalReadModel(
                    signal.Id.Value,
                    signal.CycleProfileId.Value,
                    signal.Date,
                    signal.BasalBodyTemperatureCelsius,
                    signal.OvulationTestResult,
                    signal.CervicalFluid,
                    signal.HadSex,
                    signal.Notes))]);
    }

    private static IDashboardStatisticsReadService CreateStatisticsReadService(IReadOnlyList<DashboardStatisticsBucketReadModel> buckets) {
        IDashboardStatisticsReadService service = Substitute.For<IDashboardStatisticsReadService>();
        service
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(buckets)));
        return service;
    }

    private static IDashboardStatisticsReadService CreateFailingStatisticsReadService(Error error) {
        IDashboardStatisticsReadService service = Substitute.For<IDashboardStatisticsReadService>();
        service
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(error)));
        return service;
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
