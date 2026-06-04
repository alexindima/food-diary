using FluentValidation.TestHelper;
using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;
using FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;
using FoodDiary.Application.Fasting.Commands.SkipCyclicDay;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;
using FoodDiary.Application.Fasting.Queries.GetCurrentFasting;
using FoodDiary.Application.Fasting.Queries.GetFastingHistory;
using FoodDiary.Application.Fasting.Queries.GetFastingInsights;
using FoodDiary.Application.Fasting.Queries.GetFastingOverview;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;

namespace FoodDiary.Application.Tests.Fasting;

[ExcludeFromCodeCoverage]
public class FastingValidatorTests {
    private readonly StartFastingCommandValidator _validator = new();

    [Fact]
    public async Task StartFasting_WithNullUserId_HasError() {
        var command = new StartFastingCommand(null, "F16_8", null, null, null, null, null, null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task StartFasting_WithEmptyUserId_HasError() {
        var command = new StartFastingCommand(Guid.Empty, "F16_8", null, null, null, null, null, null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task StartFasting_WithEmptyProtocol_HasError() {
        var command = new StartFastingCommand(Guid.NewGuid(), "", null, null, null, null, null, null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Protocol);
    }

    [Fact]
    public async Task StartFasting_WithValidCommand_NoErrors() {
        var command = new StartFastingCommand(Guid.NewGuid(), "F16_8", null, 16, null, null, null, null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task StartFasting_WithCyclicPlanType_WithoutProtocol_NoErrors() {
        var command = new StartFastingCommand(Guid.NewGuid(), null, "Cyclic", null, 1, 3, 16, 8, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Protocol);
    }

    [Fact]
    public async Task EndFasting_WithEmptyUserId_HasError() {
        var validator = new EndFastingCommandValidator();
        var result = await validator.TestValidateAsync(new EndFastingCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task SkipCyclicDay_WithNullUserId_HasError() {
        var validator = new SkipCyclicDayCommandValidator();
        var result = await validator.TestValidateAsync(new SkipCyclicDayCommand(null));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task PostponeCyclicDay_WithEmptyUserId_HasError() {
        var validator = new PostponeCyclicDayCommandValidator();
        var result = await validator.TestValidateAsync(new PostponeCyclicDayCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WithInvalidHours_HasError() {
        var validator = new ReduceActiveFastingTargetCommandValidator();
        var result = await validator.TestValidateAsync(new ReduceActiveFastingTargetCommand(Guid.NewGuid(), 0));

        result.ShouldHaveValidationErrorFor(x => x.ReducedHours);
    }

    [Fact]
    public async Task GetCurrentFasting_WithNullUserId_HasError() {
        var validator = new GetCurrentFastingQueryValidator();
        var result = await validator.TestValidateAsync(new GetCurrentFastingQuery(null));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task GetFastingOverview_WithEmptyUserId_HasError() {
        var validator = new GetFastingOverviewQueryValidator();
        var result = await validator.TestValidateAsync(new GetFastingOverviewQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task GetFastingStats_WithEmptyUserId_HasError() {
        var validator = new GetFastingStatsQueryValidator();
        var result = await validator.TestValidateAsync(new GetFastingStatsQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task GetFastingInsights_WithEmptyUserId_HasError() {
        var validator = new GetFastingInsightsQueryValidator();
        var result = await validator.TestValidateAsync(new GetFastingInsightsQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task GetFastingHistory_WithInvalidPagingAndRange_HasErrors() {
        var validator = new GetFastingHistoryQueryValidator();
        var result = await validator.TestValidateAsync(new GetFastingHistoryQuery(
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            0,
            99));

        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public async Task GetFastingHistory_WithValidQuery_HasNoErrors() {
        var validator = new GetFastingHistoryQueryValidator();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var result = await validator.TestValidateAsync(new GetFastingHistoryQuery(Guid.NewGuid(), from, to, 1, 10));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithNullUserId_HasInvalidTokenError() {
        var validator = new UpdateCurrentFastingCheckInCommandValidator();
        var result = await validator.TestValidateAsync(
            new UpdateCurrentFastingCheckInCommand(null, 3, 3, 3, null, null));

        result.ShouldHaveValidationErrorFor(command => command.UserId)
            .WithErrorCode("Authentication.InvalidToken");
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithInvalidLevels_HasErrors() {
        var validator = new UpdateCurrentFastingCheckInCommandValidator();
        var result = await validator.TestValidateAsync(
            new UpdateCurrentFastingCheckInCommand(Guid.NewGuid(), 0, 6, 0, null, null));

        result.ShouldHaveValidationErrorFor(command => command.HungerLevel);
        result.ShouldHaveValidationErrorFor(command => command.EnergyLevel);
        result.ShouldHaveValidationErrorFor(command => command.MoodLevel);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithInvalidSymptomsAndLongNotes_HasErrors() {
        var validator = new UpdateCurrentFastingCheckInCommandValidator();
        var result = await validator.TestValidateAsync(
            new UpdateCurrentFastingCheckInCommand(
                Guid.NewGuid(),
                3,
                3,
                3,
                ["good", "unknown", " ", "headache", "weakness", "dizziness", "cravings", "irritability", "extra"],
                new string('n', 501)));

        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Symptoms[1]", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Symptoms[2]", StringComparison.Ordinal));
        result.ShouldHaveValidationErrorFor(command => command.Symptoms);
        result.ShouldHaveValidationErrorFor(command => command.CheckInNotes);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithValidCommand_HasNoErrors() {
        var validator = new UpdateCurrentFastingCheckInCommandValidator();
        var result = await validator.TestValidateAsync(
            new UpdateCurrentFastingCheckInCommand(
                Guid.NewGuid(),
                3,
                4,
                5,
                [" good ", "Headache"],
                "steady"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
