using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;

public sealed class MarkAdminMailInboxMessageReadCommandValidator : AbstractValidator<MarkAdminMailInboxMessageReadCommand> {
    public MarkAdminMailInboxMessageReadCommandValidator() {
        RuleFor(static command => command.Id)
            .NotEmpty();
    }
}
