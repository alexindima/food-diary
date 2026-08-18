using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistSummaries;

public sealed class GetWaistSummariesQueryValidator : AbstractValidator<GetWaistSummariesQuery> {
    public GetWaistSummariesQueryValidator() {
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

        RuleFor(x => x.DateTo)
            .Must((query, dateTo) => TemporalRangePolicy.IsPeriodWithinLimit(query.DateFrom, dateTo))
            .When(x => x.DateFrom <= x.DateTo)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days.");

        RuleFor(x => x.QuantizationDays)
            .InclusiveBetween(1, TemporalRangePolicy.MaxQuantizationDays)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"QuantizationDays must be between 1 and {TemporalRangePolicy.MaxQuantizationDays}.");
    }
}
