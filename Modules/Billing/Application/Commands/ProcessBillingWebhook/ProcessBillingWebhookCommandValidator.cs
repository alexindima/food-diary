using FluentValidation;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandValidator : AbstractValidator<ProcessBillingWebhookCommand> {
    public ProcessBillingWebhookCommandValidator() {
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(BillingInputLimits.MaximumProviderLength);
    }
}
