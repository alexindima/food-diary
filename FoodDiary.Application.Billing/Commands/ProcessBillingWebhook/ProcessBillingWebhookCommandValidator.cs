using FluentValidation;
using FoodDiary.Application.Abstractions.Billing.Common;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandValidator : AbstractValidator<ProcessBillingWebhookCommand> {
    public ProcessBillingWebhookCommandValidator() {
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(BillingInputLimits.MaximumProviderLength);
    }
}
