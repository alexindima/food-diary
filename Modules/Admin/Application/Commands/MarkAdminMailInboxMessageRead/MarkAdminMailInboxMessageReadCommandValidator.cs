using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Commands.MarkAdminMailInboxMessageRead;

public sealed class MarkAdminMailInboxMessageReadCommandValidator : AbstractValidator<MarkAdminMailInboxMessageReadCommand> {
    public MarkAdminMailInboxMessageReadCommandValidator() {
        RuleFor(static command => command.Id)
            .NotEmpty();
    }
}
