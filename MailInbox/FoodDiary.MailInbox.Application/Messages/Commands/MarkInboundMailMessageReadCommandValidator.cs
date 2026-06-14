using FluentValidation;

namespace FoodDiary.MailInbox.Application.Messages.Commands;

public sealed class MarkInboundMailMessageReadCommandValidator : AbstractValidator<MarkInboundMailMessageReadCommand> {
    public MarkInboundMailMessageReadCommandValidator() {
        RuleFor(static command => command.Id)
            .NotEmpty();
    }
}
