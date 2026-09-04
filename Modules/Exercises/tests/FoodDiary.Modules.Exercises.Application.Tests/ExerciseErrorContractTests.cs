using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExerciseErrorContractTests {
    [Fact]
    public void Factory_IsOwnedByExercises() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Exercises.Application.Abstractions", typeof(ExerciseErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Exercises.Common", typeof(ExerciseErrors).Namespace));
    }

    [Fact]
    public void Errors_PreserveMissingAndInaccessibleContracts() {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        AssertError(ExerciseErrors.NotFound(id), "Exercise.NotFound",
            "Exercise entry with ID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found.", ErrorKind.NotFound);
        AssertError(ExerciseErrors.NotAccessible(id), "Exercise.NotAccessible",
            "Exercise entry with ID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found or is not accessible.", ErrorKind.NotFound);
    }

    [Fact]
    public void Errors_PreserveValidationContracts() {
        AssertError(ExerciseErrors.InvalidDuration, "Exercise.InvalidDuration",
            "Exercise duration must be positive.", ErrorKind.Validation);
        AssertError(ExerciseErrors.InvalidCalories, "Exercise.InvalidCalories",
            "Calories burned must be non-negative.", ErrorKind.Validation);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
