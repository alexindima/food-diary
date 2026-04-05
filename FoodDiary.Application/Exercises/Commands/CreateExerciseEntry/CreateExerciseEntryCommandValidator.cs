using FluentValidation;

namespace FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;

public class CreateExerciseEntryCommandValidator : AbstractValidator<CreateExerciseEntryCommand> {
    public CreateExerciseEntryCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithErrorCode("Exercise.InvalidDuration")
            .WithMessage("Duration must be positive.");

        RuleFor(x => x.CaloriesBurned)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Exercise.InvalidCalories")
            .WithMessage("Calories burned must be non-negative.");
    }
}
