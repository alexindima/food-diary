using FluentValidation.TestHelper;
using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

namespace FoodDiary.Application.Tests.Exercises;

public class ExercisesValidatorTests {
    private readonly CreateExerciseEntryCommandValidator _createValidator = new();
    private readonly GetExerciseEntriesQueryValidator _getValidator = new();

    [Fact]
    public async Task CreateExerciseEntry_WithEmptyUserId_HasError() {
        var command = new CreateExerciseEntryCommand(
            null, DateTime.UtcNow, "Running", 30, 200, null, null);
        var result = await _createValidator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithZeroDuration_HasError() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(), DateTime.UtcNow, "Running", 0, 200, null, null);
        var result = await _createValidator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.DurationMinutes);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithNegativeCalories_HasError() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(), DateTime.UtcNow, "Running", 30, -1, null, null);
        var result = await _createValidator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CaloriesBurned);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithValidCommand_NoErrors() {
        var command = new CreateExerciseEntryCommand(
            Guid.NewGuid(), DateTime.UtcNow, "Running", 30, 200, "Jog", null);
        var result = await _createValidator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GetExerciseEntries_WithEmptyUserId_HasError() {
        var query = new GetExerciseEntriesQuery(
            null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var result = await _getValidator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task GetExerciseEntries_WithValidQuery_NoErrors() {
        var query = new GetExerciseEntriesQuery(
            Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var result = await _getValidator.TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
