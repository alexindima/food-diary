using FoodDiary.Modules.Admin.Contracts.Commands.ExchangeAdminImpersonation;
using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Commands.ExchangeAdminImpersonation;

public sealed class ExchangeAdminImpersonationCommandValidator : AbstractValidator<ExchangeAdminImpersonationCommand> {
    public ExchangeAdminImpersonationCommandValidator() {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(128);
    }
}
