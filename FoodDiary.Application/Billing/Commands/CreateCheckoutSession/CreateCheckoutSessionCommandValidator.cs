using FluentValidation;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand> {
    public CreateCheckoutSessionCommandValidator() {
        RuleFor(x => x.Plan)
            .Must(IsSupportedPlan)
            .WithErrorCode("Billing.InvalidPlan")
            .WithMessage("Plan must be either 'monthly' or 'yearly'.");

        RuleFor(x => x.Provider)
            .Must(IsSupportedProvider)
            .WithErrorCode("Billing.InvalidProvider")
            .WithMessage("Provider must be a supported billing provider.");
    }

    private static bool IsSupportedPlan(string? plan) {
        var normalizedPlan = plan?.Trim();
        return string.Equals(normalizedPlan, "monthly", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedPlan, "yearly", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedProvider(string? provider) {
        if (string.IsNullOrWhiteSpace(provider)) {
            return true;
        }

        return BillingProviderNames.IsSupported(provider.Trim());
    }
}
