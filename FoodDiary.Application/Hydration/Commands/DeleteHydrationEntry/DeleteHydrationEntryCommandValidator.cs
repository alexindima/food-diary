using FluentValidation;

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
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("HydrationEntryId is invalid.");
    }
}
