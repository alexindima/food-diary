using FluentValidation;

namespace FoodDiary.MailInbox.Application.Messages.Commands.ReceiveInboundMail;

public sealed class ReceiveInboundMailCommandValidator : AbstractValidator<ReceiveInboundMailCommand> {
    public ReceiveInboundMailCommandValidator() {
        RuleFor(static command => command.Request.ToRecipients)
            .NotEmpty();

        RuleFor(static command => command.Request.RawMime)
            .NotEmpty();
    }
}
