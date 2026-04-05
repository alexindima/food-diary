using FluentValidation;

namespace FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

public class GetExerciseEntriesQueryValidator : AbstractValidator<GetExerciseEntriesQuery> {
    public GetExerciseEntriesQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");
    }
}
