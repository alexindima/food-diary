using FluentValidation;

namespace FoodDiary.MailInbox.Application.Messages.Commands.MarkInboundMailMessageRead;

public sealed class MarkInboundMailMessageReadCommandValidator : AbstractValidator<MarkInboundMailMessageReadCommand> {
    public MarkInboundMailMessageReadCommandValidator() {
        RuleFor(static command => command.Id)
            .NotEmpty();
    }
}
