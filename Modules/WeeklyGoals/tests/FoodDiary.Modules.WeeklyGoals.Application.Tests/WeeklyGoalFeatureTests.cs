using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Commands.UpsertWeeklyGoal;
using FoodDiary.Modules.WeeklyGoals.Application.Common;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Modules.WeeklyGoals.Application.Queries.GetWeeklyGoal;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyGoals.Application.Tests;

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
        IWeeklyGoalRepository readService = Substitute.For<IWeeklyGoalRepository>();
        IUserContextService userContext = CreateFailingUserContext();
        var handler = new GetWeeklyGoalQueryHandler(readService, new WeeklyGoalProgressReader(Substitute.For<ISender>()), userContext);

        Result<WeeklyGoalModel?> result = await handler.Handle(
            new GetWeeklyGoalQuery(Guid.NewGuid(), WeekStart), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await readService.DidNotReceiveWithAnyArgs().GetReadModelAsync(default, default, default);
    }

    [Fact]
    public async Task GetHandler_ReturnsGoalForNormalizedUtcWeekStart() {
        var userId = UserId.New();
        IWeeklyGoalRepository readService = Substitute.For<IWeeklyGoalRepository>();
        WeeklyGoal expected = CreateGoal(userId, reminderEnabled: false);
        readService.GetReadModelAsync(userId, WeekStartUtc, Arg.Any<CancellationToken>()).Returns(
            new WeeklyGoalReadModel(expected.Id, expected.WeekStartUtc, expected.Type, expected.TargetDays,
                expected.ReminderEnabled, expected.ReminderTimeMinutes, expected.TimeZoneOffsetMinutes));
        var handler = new GetWeeklyGoalQueryHandler(readService, new WeeklyGoalProgressReader(Substitute.For<ISender>()), CreateAccessibleUserContext());

        WeeklyGoalModel? model = ResultAssert.Success(
            await handler.Handle(new GetWeeklyGoalQuery(userId.Value, WeekStart), CancellationToken.None));

        Assert.NotNull(model);
        Assert.Equal(expected.Id.Value, model.Id);
    }

    [Fact]
    public async Task ProgressReader_CountsDistinctMealDatesAcrossWholeWeek() {
        WeeklyGoal goal = CreateGoal(UserId.New(), reminderEnabled: false);
        ISender meals = Substitute.For<ISender>();
        meals.Send(Arg.Is<ReadDistinctMealDatesQuery>(q => q.UserId == goal.UserId && q.DateFrom == WeekStartUtc && q.DateTo == WeekStartUtc.AddDays(6)), Arg.Any<CancellationToken>())
            .Returns([WeekStartUtc, WeekStartUtc.AddDays(2)]);

        int progress = await new WeeklyGoalProgressReader(meals).GetProgressDaysAsync(goal, CancellationToken.None);

        Assert.Equal(2, progress);
    }

    [Fact]
    public async Task GetHandler_WhenGoalDoesNotExist_ReturnsNullWithoutReadingProgress() {
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        ISender meals = Substitute.For<ISender>();
        var service = new GetWeeklyGoalQueryHandler(repository, new WeeklyGoalProgressReader(meals), CreateAccessibleUserContext());

        WeeklyGoalModel? model = ResultAssert.Success(await service.Handle(new GetWeeklyGoalQuery(UserId.New().Value, WeekStart), CancellationToken.None));

        Assert.Null(model);
        await meals.DidNotReceiveWithAnyArgs().Send(Arg.Any<ReadDistinctMealDatesQuery>(), default);
    }

    [Fact]
    public async Task GetHandler_WhenGoalExists_MapsCalculatedProgress() {
        var userId = UserId.New();
        WeeklyGoal goal = CreateGoal(userId, reminderEnabled: false);
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        ISender meals = Substitute.For<ISender>();
        repository.GetReadModelAsync(userId, WeekStartUtc, Arg.Any<CancellationToken>()).Returns(
            new WeeklyGoalReadModel(goal.Id, goal.WeekStartUtc, goal.Type, goal.TargetDays,
                goal.ReminderEnabled, goal.ReminderTimeMinutes, goal.TimeZoneOffsetMinutes));
        meals.Send(Arg.Is<ReadDistinctMealDatesQuery>(q => q.UserId == userId && q.DateFrom == WeekStartUtc && q.DateTo == WeekStartUtc.AddDays(6)), Arg.Any<CancellationToken>())
            .Returns([WeekStartUtc, WeekStartUtc.AddDays(2)]);
        var service = new GetWeeklyGoalQueryHandler(repository, new WeeklyGoalProgressReader(meals), CreateAccessibleUserContext());

        WeeklyGoalModel? model = ResultAssert.Success(await service.Handle(new GetWeeklyGoalQuery(userId.Value, WeekStart), CancellationToken.None));

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
        ISender meals = Substitute.For<ISender>();
        meals.Send(Arg.Is<ReadDistinctMealDatesQuery>(q => q.UserId == userId && q.DateFrom == WeekStartUtc && q.DateTo == WeekStartUtc.AddDays(6)), Arg.Any<CancellationToken>())
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
        UpsertWeeklyGoalCommandHandler handler = CreateUpsertHandler(repository, Substitute.For<ISender>());

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
            Substitute.For<ISender>());

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
            new WeeklyGoalProgressReader(Substitute.For<ISender>()),
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
            new WeeklyGoalProgressReader(Substitute.For<ISender>()),
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
        ISender meals) => new(
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
