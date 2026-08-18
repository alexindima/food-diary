using FluentValidation;
using FoodDiary.Application.Exercises.Common;

namespace FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;

public sealed class UpdateExerciseEntryCommandValidator : AbstractValidator<UpdateExerciseEntryCommand> {
    public UpdateExerciseEntryCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.EntryId)
            .NotEmpty()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Exercise entry id must not be empty.");

        RuleFor(x => x.DurationMinutes)
            .Must(value => value is null || ExerciseEntryInputValidation.IsValidDuration(value.Value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.DurationErrorMessage);

        RuleFor(x => x.CaloriesBurned)
            .Must(value => value is null || ExerciseEntryInputValidation.IsValidCalories(value.Value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.CaloriesErrorMessage);

        RuleFor(x => x.Name)
            .Must(value => ExerciseEntryInputValidation.IsValidOptionalText(value, ExerciseEntryInputValidation.MaxNameLength))
            .When(x => !x.ClearName)
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.NameErrorMessage);

        RuleFor(x => x.Notes)
            .Must(value => ExerciseEntryInputValidation.IsValidOptionalText(value, ExerciseEntryInputValidation.MaxNotesLength))
            .When(x => !x.ClearNotes)
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ExerciseEntryInputValidation.NotesErrorMessage);
    }
}
