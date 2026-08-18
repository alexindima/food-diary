using FluentValidation.TestHelper;
using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

namespace FoodDiary.Application.Tests.Exercises;

[ExcludeFromCodeCoverage]
public class ExercisesValidatorTests {
    private readonly CreateExerciseEntryCommandValidator _createValidator = new();
    private readonly UpdateExerciseEntryCommandValidator _updateValidator = new();
    private readonly GetExerciseEntriesQueryValidator _getValidator = new();

    [Fact]
    public async Task CreateExerciseEntry_WithEmptyUserId_HasError() {
        var command = new CreateExerciseEntryCommand(
            UserId: null, DateTime.UtcNow, "Running", 30, 200, Name: null, Notes: null);
        TestValidationResult<CreateExerciseEntryCommand> result = await _createValidator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithZeroDuration_HasError() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(), DateTime.UtcNow, "Running", 0, 200, Name: null, Notes: null);
        TestValidationResult<CreateExerciseEntryCommand> result = await _createValidator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.DurationMinutes);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithNegativeCalories_HasError() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(), DateTime.UtcNow, "Running", 30, -1, Name: null, Notes: null);
        TestValidationResult<CreateExerciseEntryCommand> result = await _createValidator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CaloriesBurned);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithValuesBeyondDomainLimits_HasErrors() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Running",
            1441,
            double.PositiveInfinity,
            new string('n', 257),
            new string('x', 501));

        TestValidationResult<CreateExerciseEntryCommand> result = await _createValidator.TestValidateAsync(command);

        Assert.Multiple(
            () => result.ShouldHaveValidationErrorFor(c => c.DurationMinutes),
            () => result.ShouldHaveValidationErrorFor(c => c.CaloriesBurned),
            () => result.ShouldHaveValidationErrorFor(c => c.Name),
            () => result.ShouldHaveValidationErrorFor(c => c.Notes));
    }

    [Fact]
    public async Task CreateExerciseEntry_AtDomainLimits_HasNoErrors() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Running",
            1440,
            10_000,
            new string('n', 256),
            new string('x', 500));

        TestValidationResult<CreateExerciseEntryCommand> result = await _createValidator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateExerciseEntry_WithValidCommand_NoErrors() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(), DateTime.UtcNow, "Running", 30, 200, "Jog", Notes: null);
        TestValidationResult<CreateExerciseEntryCommand> result = await _createValidator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateExerciseEntry_WithValuesBeyondDomainLimits_HasErrors() {
        var command = new UpdateExerciseEntryCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExerciseType: null,
            DurationMinutes: 1441,
            CaloriesBurned: double.NaN,
            Name: new string('n', 257),
            ClearName: false,
            Notes: new string('x', 501),
            ClearNotes: false,
            Date: null);

        TestValidationResult<UpdateExerciseEntryCommand> result = await _updateValidator.TestValidateAsync(command);

        Assert.Multiple(
            () => result.ShouldHaveValidationErrorFor(c => c.DurationMinutes),
            () => result.ShouldHaveValidationErrorFor(c => c.CaloriesBurned),
            () => result.ShouldHaveValidationErrorFor(c => c.Name),
            () => result.ShouldHaveValidationErrorFor(c => c.Notes));
    }

    [Fact]
    public async Task UpdateExerciseEntry_WhenLongTextIsCleared_HasNoTextErrors() {
        var command = new UpdateExerciseEntryCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExerciseType: null,
            DurationMinutes: null,
            CaloriesBurned: null,
            Name: new string('n', 257),
            ClearName: true,
            Notes: new string('x', 501),
            ClearNotes: true,
            Date: null);

        TestValidationResult<UpdateExerciseEntryCommand> result = await _updateValidator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Name);
        result.ShouldNotHaveValidationErrorFor(c => c.Notes);
    }

    [Fact]
    public async Task GetExerciseEntries_WithEmptyUserId_HasError() {
        var query = new GetExerciseEntriesQuery(
            UserId: null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        TestValidationResult<GetExerciseEntriesQuery> result = await _getValidator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task GetExerciseEntries_WithValidQuery_NoErrors() {
        var query = new GetExerciseEntriesQuery(
            Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        TestValidationResult<GetExerciseEntriesQuery> result = await _getValidator.TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
