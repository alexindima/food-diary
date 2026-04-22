using FluentValidation;

namespace FoodDiary.Application.Billing.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand> {
    public CreateCheckoutSessionCommandValidator() {
        RuleFor(x => x.Plan)
            .Must(static plan => string.Equals(plan, "monthly", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(plan, "yearly", StringComparison.OrdinalIgnoreCase))
            .WithErrorCode("Billing.InvalidPlan")
            .WithMessage("Plan must be either 'monthly' or 'yearly'.");
    }
}
