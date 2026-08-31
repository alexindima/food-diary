using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.ExchangeAdminImpersonation;

public sealed class ExchangeAdminImpersonationCommandValidator : AbstractValidator<ExchangeAdminImpersonationCommand> {
    public ExchangeAdminImpersonationCommandValidator() {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(128);
    }
}
