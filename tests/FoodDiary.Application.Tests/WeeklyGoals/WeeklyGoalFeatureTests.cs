using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;
using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;
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
        var validator = new UpsertWeeklyGoalCommandValidator();
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
    public async Task UpsertHandler_WhenAccessFails_ReturnsFailureWithoutRepositoryCall() {
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        var handler = new UpsertWeeklyGoalCommandHandler(
            repository,
            new WeeklyGoalProgressReader(Substitute.For<IMealActivityReadService>()),
            CreateFailingUserContext(),
            TimeProvider.System);

        Result<WeeklyGoalModel> result = await handler.Handle(
            new UpsertWeeklyGoalCommand(Guid.NewGuid(), WeekStart, 5, false, null, null), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await repository.DidNotReceiveWithAnyArgs().GetAsync(default, default, default, default);
    }

    private static UpsertWeeklyGoalCommandHandler CreateUpsertHandler(
        IWeeklyGoalRepository repository,
        IMealActivityReadService meals) => new(
            repository,
            new WeeklyGoalProgressReader(meals),
            CreateAccessibleUserContext(),
            TimeProvider.System);

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
