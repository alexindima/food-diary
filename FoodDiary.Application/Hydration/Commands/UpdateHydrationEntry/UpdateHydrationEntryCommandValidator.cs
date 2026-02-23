using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public class UpdateHydrationEntryCommandValidator : AbstractValidator<UpdateHydrationEntryCommand> {
    public UpdateHydrationEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(c => c.HydrationEntryId)
            .NotEqual(HydrationEntryId.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("HydrationEntryId is invalid.");

        RuleFor(c => c.AmountMl)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000)
            .When(c => c.AmountMl.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AmountMl must be in range [1, 10000].");
    }
}
