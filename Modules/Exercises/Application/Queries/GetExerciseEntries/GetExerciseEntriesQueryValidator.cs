using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

public sealed class GetExerciseEntriesQueryValidator : AbstractValidator<GetExerciseEntriesQuery> {
    public GetExerciseEntriesQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DateFrom must be earlier than or equal to DateTo.");

        RuleFor(x => x.DateTo)
            .Must((query, dateTo) => TemporalRangePolicy.IsPeriodWithinLimit(query.DateFrom, dateTo))
            .When(query => query.DateFrom <= query.DateTo)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days.");
    }
}
