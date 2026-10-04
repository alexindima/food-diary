using FluentValidation;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Statistics.Application.Common;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetStatistics;

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
            .Must((query, dateTo) => StatisticsCalendarRangePolicy.IsPeriodWithinLimit(query.DateFrom, dateTo, query.TimeZoneId))
            .When(x => x.DateFrom <= x.DateTo && StatisticsCalendarRangePolicy.IsTimeZoneValid(x.TimeZoneId))
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days.");

        RuleFor(x => x.QuantizationDays)
            .InclusiveBetween(1, TemporalRangePolicy.MaxQuantizationDays);

        RuleFor(x => x.TimeZoneId)
            .Must(StatisticsCalendarRangePolicy.IsTimeZoneValid)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Provide a valid calendar time zone.");
    }
}
