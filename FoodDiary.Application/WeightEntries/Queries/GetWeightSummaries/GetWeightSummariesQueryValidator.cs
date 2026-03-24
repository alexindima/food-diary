using FluentValidation;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;

public class GetWeightSummariesQueryValidator : AbstractValidator<GetWeightSummariesQuery> {
    public GetWeightSummariesQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DateFrom must be earlier than or equal to DateTo.");

        RuleFor(x => x.QuantizationDays)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("QuantizationDays must be greater than zero.");
    }
}
