using FluentValidation;
using FoodDiary.Modules.Wearables.Application.Common;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableDailySummary;

public sealed class GetWearableDailySummaryQueryValidator : AbstractValidator<GetWearableDailySummaryQuery> {
    public GetWearableDailySummaryQueryValidator(TimeProvider timeProvider) {
        RuleFor(query => query.Date)
            .Must(date => WearableDateRules.IsSupported(date, timeProvider))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Date must be between 1970-01-01 and the current UTC date.");
    }
}
