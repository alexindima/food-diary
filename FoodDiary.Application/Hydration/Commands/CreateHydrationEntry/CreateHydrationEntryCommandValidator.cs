using FluentValidation;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public class CreateHydrationEntryCommandValidator : AbstractValidator<CreateHydrationEntryCommand> {
    public CreateHydrationEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(c => c.AmountMl)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AmountMl must be in range [1, 10000].");
    }
}
