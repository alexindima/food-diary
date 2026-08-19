using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Export.Models;

namespace FoodDiary.Application.Export.Queries.ExportCycle;

public sealed class ExportCycleQueryValidator : AbstractValidator<ExportCycleQuery> {
    public ExportCycleQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("User ID is required.");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DateFrom must be less than or equal to DateTo.");

        RuleFor(x => x)
            .Must(x => x.DateTo.DayNumber - x.DateFrom.DayNumber <= 366)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Export range must not exceed one year.");

        RuleFor(x => x.Scope)
            .IsInEnum()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Export scope is invalid.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .When(x => x.Scope == CycleExportScope.Sensitive)
            .WithErrorCode("Validation.Required")
            .WithMessage("Current password is required for a sensitive cycle export.")
            .MaximumLength(AuthenticationInputLimits.MaximumPasswordLength)
            .When(x => x.Scope == CycleExportScope.Sensitive)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Current password must not exceed {AuthenticationInputLimits.MaximumPasswordLength} characters.");

        RuleFor(x => x.TimeZoneOffsetMinutes)
            .InclusiveBetween(
                ExportInputLimits.MinimumTimeZoneOffsetMinutes,
                ExportInputLimits.MaximumTimeZoneOffsetMinutes)
            .When(x => x.TimeZoneOffsetMinutes.HasValue);
    }
}
