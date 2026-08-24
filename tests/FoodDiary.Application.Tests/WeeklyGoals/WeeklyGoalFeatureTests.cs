using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;
using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;
using FoodDiary.Application.WeeklyGoals.Services;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.WeeklyGoals;

#pragma warning disable MA0003

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalFeatureTests {
    private static readonly DateOnly WeekStart = new(2026, 8, 10);
    private static readonly DateTime WeekStartUtc = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RequestModels_ExposeConstructorValues() {
        var userId = Guid.NewGuid();
        var query = new GetWeeklyGoalQuery(userId, WeekStart);
        var command = new UpsertWeeklyGoalCommand(userId, WeekStart, 5, true, new TimeOnly(9, 30), 240);
        var model = new WeeklyGoalModel(Guid.NewGuid(), WeekStart, "DiaryLogging", 5, 3, false, true, new TimeOnly(9, 30), 240);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(WeekStart, query.WeekStart),
            () => Assert.Equal(5, command.TargetDays),
            () => Assert.True(command.ReminderEnabled),
            () => Assert.NotEqual(Guid.Empty, model.Id),
            () => Assert.Equal(WeekStart, model.WeekStart),
            () => Assert.Equal(3, model.ProgressDays),
            () => Assert.Equal("DiaryLogging", model.Type));
    }

    [Fact]
    public async Task GetValidator_RequiresMonday() {
        ValidationResult valid = await new GetWeeklyGoalQueryValidator().ValidateAsync(
            new GetWeeklyGoalQuery(null, WeekStart));
        ValidationResult invalid = await new GetWeeklyGoalQueryValidator().ValidateAsync(
            new GetWeeklyGoalQuery(null, WeekStart.AddDays(1)));

        Assert.Multiple(() => Assert.True(valid.IsValid), () => Assert.False(invalid.IsValid));
    }

    [Fact]
    public async Task UpsertValidator_CoversSupportedTargetsAndReminderRequirements() {
        var validator = new UpsertWeeklyGoalCommandValidator(new FixedTimeProvider(WeekStartUtc.AddHours(12)));
        ValidationResult valid = await validator.ValidateAsync(
            new UpsertWeeklyGoalCommand(null, WeekStart, 3, false, null, null));
        ValidationResult invalid = await validator.ValidateAsync(
            new UpsertWeeklyGoalCommand(null, WeekStart.AddDays(1), 4, true, null, 841));

        Assert.Multiple(
            () => Assert.True(valid.IsValid),
            () => Assert.Contains(invalid.Errors, error => string.Equals(error.PropertyName, "WeekStart", StringComparison.Ordinal)),
            () => Assert.Contains(invalid.Errors, error => string.Equals(error.PropertyName, "TargetDays", StringComparison.Ordinal)),
            () => Assert.Contains(invalid.Errors, error => string.Equals(error.PropertyName, "ReminderTime", StringComparison.Ordinal)),
            () => Assert.Contains(invalid.Errors, error => string.Equals(error.PropertyName, "TimeZoneOffsetMinutes", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(-14, false)]
    [InlineData(-7, true)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(14, false)]
    public async Task UpsertValidator_AllowsOnlyAdjacentCurrentWeeks(int dayOffset, bool expectedValid) {
        var validator = new UpsertWeeklyGoalCommandValidator(new FixedTimeProvider(WeekStartUtc.AddDays(2)));

        ValidationResult result = await validator.ValidateAsync(
            new UpsertWeeklyGoalCommand(null, WeekStart.AddDays(dayOffset), 5, false, null, null));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public async Task GetHandler_WhenAccessFails_ReturnsFailureWithoutReadingGoal() {
        IWeeklyGoalReadService readService = Substitute.For<IWeeklyGoalReadService>();
        IUserContextService userContext = CreateFailingUserContext();
        var handler = new GetWeeklyGoalQueryHandler(readService, userContext);

        Result<WeeklyGoalModel?> result = await handler.Handle(
            new GetWeeklyGoalQuery(Guid.NewGuid(), WeekStart), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await readService.DidNotReceiveWithAnyArgs().GetAsync(default, default, default);
    }

    [Fact]
    public async Task GetHandler_ReturnsGoalForNormalizedUtcWeekStart() {
        var userId = UserId.New();
        IWeeklyGoalReadService readService = Substitute.For<IWeeklyGoalReadService>();
        var expected = new WeeklyGoalModel(Guid.NewGuid(), WeekStart, "DiaryLogging", 5, 2, false, false, null, null);
        readService.GetAsync(userId, WeekStartUtc, Arg.Any<CancellationToken>()).Returns(expected);
        var handler = new GetWeeklyGoalQueryHandler(readService, CreateAccessibleUserContext());

        WeeklyGoalModel? model = ResultAssert.Success(
            await handler.Handle(new GetWeeklyGoalQuery(userId.Value, WeekStart), CancellationToken.None));

        Assert.Same(expected, model);
    }

    [Fact]
    public async Task ProgressReader_CountsDistinctMealDatesAcrossWholeWeek() {
        WeeklyGoal goal = CreateGoal(UserId.New(), reminderEnabled: false);
        IMealActivityReadService meals = Substitute.For<IMealActivityReadService>();
        meals.GetDistinctMealDatesAsync(goal.UserId, WeekStartUtc, WeekStartUtc.AddDays(6), Arg.Any<CancellationToken>())
            .Returns([WeekStartUtc, WeekStartUtc.AddDays(2)]);

        int progress = await new WeeklyGoalProgressReader(meals).GetProgressDaysAsync(goal, CancellationToken.None);

        Assert.Equal(2, progress);
    }

    [Fact]
    public async Task ReadService_WhenGoalDoesNotExist_ReturnsNullWithoutReadingProgress() {
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        IMealActivityReadService meals = Substitute.For<IMealActivityReadService>();
        var service = new WeeklyGoalReadService(repository, new WeeklyGoalProgressReader(meals));

        WeeklyGoalModel? model = await service.GetAsync(UserId.New(), WeekStartUtc, CancellationToken.None);

        Assert.Null(model);
        await meals.DidNotReceiveWithAnyArgs().GetDistinctMealDatesAsync(default, default, default, default);
    }

    [Fact]
    public async Task ReadService_WhenGoalExists_MapsCalculatedProgress() {
        var userId = UserId.New();
        WeeklyGoal goal = CreateGoal(userId, reminderEnabled: false);
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        IMealActivityReadService meals = Substitute.For<IMealActivityReadService>();
        repository.GetAsync(userId, WeekStartUtc, false, Arg.Any<CancellationToken>()).Returns(goal);
        meals.GetDistinctMealDatesAsync(userId, WeekStartUtc, WeekStartUtc.AddDays(6), Arg.Any<CancellationToken>())
            .Returns([WeekStartUtc, WeekStartUtc.AddDays(2)]);
        var service = new WeeklyGoalReadService(repository, new WeeklyGoalProgressReader(meals));

        WeeklyGoalModel? model = await service.GetAsync(userId, WeekStartUtc, CancellationToken.None);

        Assert.NotNull(model);
        Assert.Multiple(
            () => Assert.Equal(2, model.ProgressDays),
            () => Assert.Equal(5, model.TargetDays),
            () => Assert.False(model.IsCompleted));
    }

    [Fact]
    public async Task UpsertHandler_WhenGoalDoesNotExist_CreatesAndMapsReminder() {
        var userId = UserId.New();
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        IMealActivityReadService meals = Substitute.For<IMealActivityReadService>();
        meals.GetDistinctMealDatesAsync(userId, WeekStartUtc, WeekStartUtc.AddDays(6), Arg.Any<CancellationToken>())
            .Returns([WeekStartUtc, WeekStartUtc.AddDays(1), WeekStartUtc.AddDays(2)]);
        UpsertWeeklyGoalCommandHandler handler = CreateUpsertHandler(repository, meals);

        WeeklyGoalModel model = ResultAssert.Success(await handler.Handle(
            new UpsertWeeklyGoalCommand(userId.Value, WeekStart, 3, true, new TimeOnly(9, 30), 240),
            CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(3, model.ProgressDays),
            () => Assert.True(model.IsCompleted),
            () => Assert.Equal(new TimeOnly(9, 30), model.ReminderTime),
            () => Assert.Equal(240, model.TimeZoneOffsetMinutes));
        await repository.Received(1).AddAsync(Arg.Any<WeeklyGoal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertHandler_WhenGoalExists_UpdatesAndMapsDisabledReminder() {
        var userId = UserId.New();
        WeeklyGoal goal = CreateGoal(userId, reminderEnabled: true);
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        repository.GetAsync(userId, WeekStartUtc, true, Arg.Any<CancellationToken>()).Returns(goal);
        UpsertWeeklyGoalCommandHandler handler = CreateUpsertHandler(repository, Substitute.For<IMealActivityReadService>());

        WeeklyGoalModel model = ResultAssert.Success(await handler.Handle(
            new UpsertWeeklyGoalCommand(userId.Value, WeekStart, 7, false, null, null), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(7, model.TargetDays),
            () => Assert.False(model.ReminderEnabled),
            () => Assert.Null(model.ReminderTime),
            () => Assert.Null(model.TimeZoneOffsetMinutes));
        await repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task UpsertHandler_WhenRequestIsRetried_PreservesReminderDeduplicationDate() {
        var userId = UserId.New();
        WeeklyGoal goal = CreateGoal(userId, reminderEnabled: true);
        var reminderDate = new DateOnly(2026, 8, 10);
        goal.MarkReminderSent(reminderDate, WeekStartUtc.AddHours(17));
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        repository.GetAsync(userId, WeekStartUtc, true, Arg.Any<CancellationToken>()).Returns(goal);
        UpsertWeeklyGoalCommandHandler handler = CreateUpsertHandler(
            repository,
            Substitute.For<IMealActivityReadService>());

        Result<WeeklyGoalModel> result = await handler.Handle(
            new UpsertWeeklyGoalCommand(userId.Value, WeekStart, 5, true, new TimeOnly(9, 30), 240),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(reminderDate, goal.LastReminderLocalDate);
    }

    [Fact]
    public async Task UpsertHandler_WhenAccessFails_ReturnsFailureWithoutRepositoryCall() {
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        var handler = new UpsertWeeklyGoalCommandHandler(
            repository,
            new InlineWeeklyGoalTransactionRunner(),
            new WeeklyGoalProgressReader(Substitute.For<IMealActivityReadService>()),
            CreateFailingUserContext(),
            TimeProvider.System);

        Result<WeeklyGoalModel> result = await handler.Handle(
            new UpsertWeeklyGoalCommand(Guid.NewGuid(), WeekStart, 5, false, null, null), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await repository.DidNotReceiveWithAnyArgs().GetAsync(default, default, default, default);
    }

    [Fact]
    public async Task UpsertHandler_WhenWeekIsOutsideWritableWindow_RejectsBeforeTransaction() {
        var userId = UserId.New();
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        IWeeklyGoalTransactionRunner transactionRunner = Substitute.For<IWeeklyGoalTransactionRunner>();
        var handler = new UpsertWeeklyGoalCommandHandler(
            repository,
            transactionRunner,
            new WeeklyGoalProgressReader(Substitute.For<IMealActivityReadService>()),
            CreateAccessibleUserContext(),
            new FixedTimeProvider(WeekStartUtc.AddDays(2)));

        Result<WeeklyGoalModel> result = await handler.Handle(
            new UpsertWeeklyGoalCommand(userId.Value, WeekStart.AddDays(-14), 5, false, null, null),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await transactionRunner.DidNotReceiveWithAnyArgs().ExecuteSerializedAsync<Result<WeeklyGoalModel>>(
            default, default, default!, default);
    }

    private static UpsertWeeklyGoalCommandHandler CreateUpsertHandler(
        IWeeklyGoalRepository repository,
        IMealActivityReadService meals) => new(
            repository,
            new InlineWeeklyGoalTransactionRunner(),
            new WeeklyGoalProgressReader(meals),
            CreateAccessibleUserContext(),
            new FixedTimeProvider(WeekStartUtc.AddHours(12)));

    [ExcludeFromCodeCoverage]
    private sealed class InlineWeeklyGoalTransactionRunner : IWeeklyGoalTransactionRunner {
        public Task<T> ExecuteSerializedAsync<T>(
            UserId userId,
            DateTime weekStartUtc,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static WeeklyGoal CreateGoal(UserId userId, bool reminderEnabled) => WeeklyGoal.Create(
        userId,
        WeekStartUtc,
        WeeklyGoalType.DiaryLogging,
        5,
        reminderEnabled,
        reminderEnabled ? 570 : null,
        reminderEnabled ? 240 : null);

    private static IUserContextService CreateAccessibleUserContext() {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        return service;
    }

    private static IUserContextService CreateFailingUserContext() {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(Errors.Validation.Invalid("UserId", "Access denied.")));
        return service;
    }
}
