using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Tests.Dashboard;

[ExcludeFromCodeCoverage]
public sealed class DashboardSnapshotBuilderTests {
    [Fact]
    public async Task BuildAsync_WithEmptyUserId_ReturnsValidationFailure() {
        var builder = new DashboardSnapshotBuilder(
            new StubSender(),
            new StubUserRepository(),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                Guid.Empty,
                new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
                DateTo: null,
                "en",
                7,
                1,
                10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildAsync_WithDateToBeforeDate_ReturnsValidationFailure() {
        var user = User.Create("dashboard-date-range@example.com", "hash");
        var builder = new DashboardSnapshotBuilder(
            new StubSender(),
            new AccessibleUserRepository(user),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                user.Id.Value,
                new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc),
                "en",
                7,
                1,
                10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("DateTo", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildAsync_WhenUserIsMissing_ReturnsAccessFailure() {
        var existingUser = User.Create("dashboard-existing@example.com", "hash");
        var builder = new DashboardSnapshotBuilder(
            new StubSender(),
            new AccessibleUserRepository(existingUser),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                Guid.NewGuid(),
                new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
                DateTo: null,
                "en",
                7,
                1,
                10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task BuildAsync_WithInvalidDashboardLayoutJson_ReturnsSnapshotWithoutLayout() {
        var user = User.Create("dashboard-invalid-layout@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: "{"));
        var builder = new DashboardSnapshotBuilder(
            new StubSender(),
            new AccessibleUserRepository(user),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                user.Id.Value,
                new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
                DateTo: null,
                "",
                TrendDays: 0,
                Page: 0,
                PageSize: 0,
                Sections: new DashboardSnapshotSections(
                    IncludeStatistics: false,
                    IncludeMeals: false,
                    IncludeWeight: false,
                    IncludeWaist: false,
                    IncludeHydration: false,
                    IncludeFasting: false,
                    IncludeAdvice: false,
                    IncludeLayout: true,
                    IncludeExercise: false,
                    IncludeTdee: false,
                    IncludeCycle: false)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value.DashboardLayout);
        Assert.Equal(0, result.Value.CaloriesBurned);
    }

    [Fact]
    public async Task BuildAsync_ForPastDate_UsesMeasurementEntriesAvailableBySelectedDate() {
        var user = User.Create("dashboard-measurements@example.com", "hash");
        UserId userId = user.Id;
        var selectedDate = new DateTime(2026, 3, 20, 12, 0, 0, DateTimeKind.Utc);
        DateTime futureDate = selectedDate.AddDays(1);
        DateTime previousDate = selectedDate.AddDays(-1);
        var weightRepository = new FilteringWeightEntryRepository([
            WeightEntry.Create(userId, futureDate, 90),
            WeightEntry.Create(userId, selectedDate, 82),
            WeightEntry.Create(userId, previousDate, 83),
        ]);
        var waistRepository = new FilteringWaistEntryRepository([
            WaistEntry.Create(userId, futureDate, 96),
            WaistEntry.Create(userId, selectedDate, 91),
            WaistEntry.Create(userId, previousDate, 92),
        ]);
        var builder = new DashboardSnapshotBuilder(
            new EmptyTrendSender(),
            new AccessibleUserRepository(user),
            weightRepository,
            waistRepository,
            new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                userId.Value,
                selectedDate,
                DateTo: null,
                "en",
                7,
                1,
                10,
                new DashboardSnapshotSections(
                    IncludeStatistics: false,
                    IncludeMeals: false,
                    IncludeWeight: true,
                    IncludeWaist: true,
                    IncludeHydration: false,
                    IncludeFasting: false,
                    IncludeAdvice: false,
                    IncludeLayout: false,
                    IncludeExercise: false,
                    IncludeTdee: false,
                    IncludeCycle: false)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(selectedDate.Date, weightRepository.LastDateTo?.Date);
        Assert.Equal(selectedDate.Date, waistRepository.LastDateTo?.Date);
        Assert.Equal(82, result.Value.Weight.Latest?.Weight);
        Assert.Equal(83, result.Value.Weight.Previous?.Weight);
        Assert.Equal(91, result.Value.Waist.Latest?.Circumference);
        Assert.Equal(92, result.Value.Waist.Previous?.Circumference);
    }

    [Fact]
    public async Task BuildAsync_NormalizesMealPagingBeforeLoadingConsumptions() {
        var user = User.Create("dashboard-paging@example.com", "hash");
        var sender = new RecordingConsumptionsSender();
        var builder = new DashboardSnapshotBuilder(
            sender,
            new AccessibleUserRepository(user),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            new DashboardSnapshotRequest(
                user.Id.Value,
                new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
                DateTo: null,
                "en",
                7,
                Page: 0,
                PageSize: 500,
                Sections: new DashboardSnapshotSections(
                    IncludeStatistics: false,
                    IncludeMeals: true,
                    IncludeWeight: false,
                    IncludeWaist: false,
                    IncludeHydration: false,
                    IncludeFasting: false,
                    IncludeAdvice: false,
                    IncludeLayout: false,
                    IncludeExercise: false,
                    IncludeTdee: false,
                    IncludeCycle: false)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(sender.LastConsumptionsQuery);
        Assert.Equal(1, sender.LastConsumptionsQuery.Page);
        Assert.Equal(100, sender.LastConsumptionsQuery.Limit);
    }

    [Fact]
    public async Task BuildAsync_WhenStatisticsQueryFails_ReturnsFailure() {
        var user = User.Create("dashboard-statistics-failure@example.com", "hash");
        var sender = new ConfigurableDashboardSender {
            FirstStatisticsError = Errors.Validation.Invalid("statistics", "Statistics failed."),
        };
        DashboardSnapshotBuilder builder = CreateBuilder(user, sender);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            CreateRequest(user.Id.Value, Sections(includeStatistics: true)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Single(sender.StatisticsQueries);
    }

    [Fact]
    public async Task BuildAsync_WhenWeeklyStatisticsQueryFails_ReturnsFailure() {
        var user = User.Create("dashboard-weekly-statistics-failure@example.com", "hash");
        var date = new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var sender = new ConfigurableDashboardSender {
            SecondStatisticsError = Errors.Validation.Invalid("weeklyStatistics", "Weekly statistics failed."),
        };
        DashboardSnapshotBuilder builder = CreateBuilder(user, sender);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            CreateRequest(user.Id.Value, Sections(includeStatistics: true), date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal(2, sender.StatisticsQueries.Count);
        Assert.Equal(date.Date.AddDays(-6), sender.StatisticsQueries[1].DateFrom);
    }

    [Fact]
    public async Task BuildAsync_WhenMealsSectionFails_ReturnsFailure() {
        var user = User.Create("dashboard-meals-failure@example.com", "hash");
        var sender = new ConfigurableDashboardSender {
            MealsError = Errors.Validation.Invalid("meals", "Meals failed."),
        };
        DashboardSnapshotBuilder builder = CreateBuilder(user, sender);

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            CreateRequest(user.Id.Value, Sections(includeStatistics: true, includeMeals: true)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.NotNull(sender.LastConsumptionsQuery);
    }

    [Fact]
    public async Task BuildAsync_WithHydrationTotals_ReturnsSummedHydration() {
        var user = User.Create("dashboard-hydration@example.com", "hash");
        user.UpdateGoals(waterGoal: 1800);
        var date = new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        DashboardSnapshotBuilder builder = CreateBuilder(
            user,
            new ConfigurableDashboardSender(),
            new StubHydrationEntryRepository([
                (date.Date, 500),
                (date.Date.AddDays(1), 700),
            ]));

        Result<DashboardSnapshotModel> result = await builder.BuildAsync(
            CreateRequest(user.Id.Value, Sections(includeHydration: true), date, date.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value.Hydration);
        Assert.Equal(1200, result.Value.Hydration!.TotalMl);
        Assert.Equal(3600, result.Value.Hydration.GoalMl);
    }

    private static DashboardSnapshotBuilder CreateBuilder(
        User user,
        ISender sender,
        IHydrationEntryRepository? hydrationEntryRepository = null) =>
        new(
            sender,
            new AccessibleUserRepository(user),
            new StubWeightEntryRepository(),
            new StubWaistEntryRepository(),
            hydrationEntryRepository ?? new StubHydrationEntryRepository(),
            new StubFastingOccurrenceRepository(),
            new StubExerciseEntryRepository(),
            NullLogger<DashboardSnapshotBuilder>.Instance);

    private static DashboardSnapshotRequest CreateRequest(
        Guid userId,
        DashboardSnapshotSections sections,
        DateTime? date = null,
        DateTime? dateTo = null) =>
        new(
            userId,
            date ?? new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc),
            dateTo,
            "en",
            TrendDays: 7,
            Page: 1,
            PageSize: 10,
            Sections: sections);

    private static DashboardSnapshotSections Sections(
        bool includeStatistics = false,
        bool includeMeals = false,
        bool includeHydration = false) =>
        new(
            IncludeStatistics: includeStatistics,
            IncludeMeals: includeMeals,
            IncludeWeight: false,
            IncludeWaist: false,
            IncludeHydration: includeHydration,
            IncludeFasting: false,
            IncludeAdvice: false,
            IncludeLayout: false,
            IncludeExercise: false,
            IncludeTdee: false,
            IncludeCycle: false);

    [ExcludeFromCodeCoverage]
    private sealed class ConfigurableDashboardSender : ISender {
        public Error? FirstStatisticsError { get; init; }
        public Error? SecondStatisticsError { get; init; }
        public Error? MealsError { get; init; }
        public List<GetStatisticsQuery> StatisticsQueries { get; } = [];
        public GetConsumptionsQuery? LastConsumptionsQuery { get; private set; }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            if (request is GetStatisticsQuery statisticsQuery) {
                StatisticsQueries.Add(statisticsQuery);
                Error? error = StatisticsQueries.Count == 1 ? FirstStatisticsError : SecondStatisticsError;
                return error is not null
                    ? Task.FromResult((TResponse)(object)Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(error))
                    : Task.FromResult((TResponse)(object)Result.Success<IReadOnlyList<AggregatedStatisticsModel>>([
                        new AggregatedStatisticsModel(
                            statisticsQuery.DateFrom,
                            statisticsQuery.DateTo,
                            TotalCalories: 1800,
                            AverageProteins: 100,
                            AverageFats: 60,
                            AverageCarbs: 200,
                            AverageFiber: 25),
                    ]));
            }

            if (request is GetConsumptionsQuery consumptionsQuery) {
                LastConsumptionsQuery = consumptionsQuery;
                if (MealsError is not null) {
                    return Task.FromResult((TResponse)(object)Result.Failure<PagedResponse<ConsumptionModel>>(MealsError));
                }

                var response = new PagedResponse<ConsumptionModel>([], consumptionsQuery.Page, consumptionsQuery.Limit, 0, 0);
                return Task.FromResult((TResponse)(object)Result.Success(response));
            }

            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class EmptyTrendSender : ISender {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            if (request is GetWeightSummariesQuery) {
                return Task.FromResult((TResponse)(object)Result.Success<IReadOnlyList<WeightEntrySummaryModel>>([]));
            }

            if (request is GetWaistSummariesQuery) {
                return Task.FromResult((TResponse)(object)Result.Success<IReadOnlyList<WaistEntrySummaryModel>>([]));
            }

            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingConsumptionsSender : ISender {
        public GetConsumptionsQuery? LastConsumptionsQuery { get; private set; }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            if (request is GetConsumptionsQuery query) {
                LastConsumptionsQuery = query;
                var response = new PagedResponse<ConsumptionModel>([], query.Page, query.Limit, 0, 0);
                return Task.FromResult((TResponse)(object)Result.Success(response));
            }

            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class AccessibleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FilteringWeightEntryRepository(IReadOnlyList<WeightEntry> entries) : IWeightEntryRepository {
        public DateTime? LastDateTo { get; private set; }

        public Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByIdAsync(WeightEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken = default) {
            LastDateTo = dateTo;
            IEnumerable<WeightEntry> filtered = entries
                .Where(entry => entry.UserId == userId)
                .Where(entry => !dateFrom.HasValue || entry.Date.Date >= dateFrom.Value.Date)
                .Where(entry => !dateTo.HasValue || entry.Date.Date <= dateTo.Value.Date);
            filtered = descending ? filtered.OrderByDescending(entry => entry.Date) : filtered.OrderBy(entry => entry.Date);
            if (limit.HasValue) {
                filtered = filtered.Take(limit.Value);
            }

            return Task.FromResult<IReadOnlyList<WeightEntry>>(filtered.ToList());
        }

        public Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FilteringWaistEntryRepository(IReadOnlyList<WaistEntry> entries) : IWaistEntryRepository {
        public DateTime? LastDateTo { get; private set; }

        public Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByIdAsync(WaistEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken = default) {
            LastDateTo = dateTo;
            IEnumerable<WaistEntry> filtered = entries
                .Where(entry => entry.UserId == userId)
                .Where(entry => !dateFrom.HasValue || entry.Date.Date >= dateFrom.Value.Date)
                .Where(entry => !dateTo.HasValue || entry.Date.Date <= dateTo.Value.Date);
            filtered = descending ? filtered.OrderByDescending(entry => entry.Date) : filtered.OrderBy(entry => entry.Date);
            if (limit.HasValue) {
                filtered = filtered.Take(limit.Value);
            }

            return Task.FromResult<IReadOnlyList<WaistEntry>>(filtered.ToList());
        }

        public Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubSender : ISender {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubWeightEntryRepository : IWeightEntryRepository {
        public Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByIdAsync(WeightEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WeightEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(UserId userId, DateTime? dateFrom, DateTime? dateTo, int? limit, bool descending, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubExerciseEntryRepository : IExerciseEntryRepository {
        public Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(ExerciseEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExerciseEntry?> GetByIdAsync(ExerciseEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ExerciseEntry>> GetByDateRangeAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<double> GetTotalCaloriesBurnedAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => Task.FromResult(0.0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubHydrationEntryRepository(IReadOnlyList<(DateTime Date, int TotalMl)>? totals = null) : IHydrationEntryRepository {
        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(totals ?? []);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubFastingOccurrenceRepository : IFastingOccurrenceRepository {
        public Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult<FastingOccurrence?>(null);
        public Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FastingOccurrence>>([]);
        public Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(FastingPlanId planId, bool includeCompleted = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(
            UserId userId,
            DateTime? from = null,
            DateTime? to = null,
            FastingOccurrenceStatus? status = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? from = null,
            DateTime? to = null,
            FastingOccurrenceStatus? status = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)>(([], 0));
        public Task AddAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubWaistEntryRepository : IWaistEntryRepository {
        public Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByIdAsync(WaistEntryId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WaistEntry?> GetByDateAsync(UserId userId, DateTime date, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(UserId userId, DateTime? dateFrom, DateTime? dateTo, int? limit, bool descending, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
