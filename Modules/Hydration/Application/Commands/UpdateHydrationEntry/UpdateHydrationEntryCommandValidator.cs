using FluentValidation;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public sealed class UpdateHydrationEntryCommandValidator : AbstractValidator<UpdateHydrationEntryCommand> {
    public UpdateHydrationEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(c => c.HydrationEntryId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("HydrationEntryId is invalid.");

        RuleFor(c => c.AmountMl)
            .GreaterThan(0)
            .LessThanOrEqualTo(HydrationEntry.MaximumAmountMl)
            .When(c => c.AmountMl.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AmountMl must be in range [1, 10000].");

        RuleFor(c => c)
            .Must(c => c.TimestampUtc.HasValue || c.AmountMl.HasValue)
            .WithErrorCode("Validation.Required")
            .WithMessage("At least one hydration entry field must be provided.");
    }
}
