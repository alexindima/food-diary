using FluentValidation;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

public sealed class ExportDiaryQueryValidator : AbstractValidator<ExportDiaryQuery> {
    public ExportDiaryQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("User ID is required.");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DateFrom must be less than or equal to DateTo.");

        RuleFor(x => x)
            .Must(x => (x.DateTo - x.DateFrom).TotalDays <= 366)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Export range must not exceed one year.");

        RuleFor(x => x.DateFrom)
            .Must((query, ignoredDateFrom) => ExportDiaryDateRangePolicy.TryResolve(
                query.DateFrom,
                query.DateTo,
                query.TimeZoneOffsetMinutes,
                out _,
                out _,
                out _))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Export dates cannot be represented with the requested time-zone offset.");
    }
}
