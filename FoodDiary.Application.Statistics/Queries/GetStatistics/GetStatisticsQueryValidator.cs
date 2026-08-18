using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public sealed class GetStatisticsQueryValidator : AbstractValidator<GetStatisticsQuery> {
    public GetStatisticsQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
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
            .InclusiveBetween(1, TemporalRangePolicy.MaxQuantizationDays);
    }
}
