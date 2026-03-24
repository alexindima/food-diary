using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public class DeleteHydrationEntryCommandValidator : AbstractValidator<DeleteHydrationEntryCommand> {
    public DeleteHydrationEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(c => c.HydrationEntryId)
            .NotEqual(HydrationEntryId.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("HydrationEntryId is invalid.");
    }
}
