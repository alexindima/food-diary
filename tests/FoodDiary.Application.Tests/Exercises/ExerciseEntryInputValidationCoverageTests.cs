using FoodDiary.Application.Exercises.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Exercises;

[ExcludeFromCodeCoverage]
public sealed class ExerciseEntryInputValidationCoverageTests {
    [Fact]
    public void GetError_WhenClearFlagsAreSet_IgnoresOversizedOptionalText() {
        string oversizedName = new('n', ExerciseEntryInputValidation.MaxNameLength + 1);
        string oversizedNotes = new('x', ExerciseEntryInputValidation.MaxNotesLength + 1);

        Error? error = ExerciseEntryInputValidation.GetError(
            durationMinutes: null,
            caloriesBurned: null,
            oversizedName,
            clearName: true,
            oversizedNotes,
            clearNotes: true);

        Assert.Null(error);
    }
}
