using FluentValidation;
using FoodDiary.Application.Exercises.Common;

namespace FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;

public sealed class CreateExerciseEntryCommandValidator : AbstractValidator<CreateExerciseEntryCommand> {
    public CreateExerciseEntryCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.DurationMinutes)
            .Must(ExerciseEntryInputValidation.IsValidDuration)
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.DurationErrorMessage);

        RuleFor(x => x.CaloriesBurned)
            .Must(ExerciseEntryInputValidation.IsValidCalories)
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.CaloriesErrorMessage);

        RuleFor(x => x.Name)
            .Must(value => ExerciseEntryInputValidation.IsValidOptionalText(value, ExerciseEntryInputValidation.MaxNameLength))
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.NameErrorMessage);

        RuleFor(x => x.Notes)
            .Must(value => ExerciseEntryInputValidation.IsValidOptionalText(value, ExerciseEntryInputValidation.MaxNotesLength))
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.NotesErrorMessage);
    }
}
