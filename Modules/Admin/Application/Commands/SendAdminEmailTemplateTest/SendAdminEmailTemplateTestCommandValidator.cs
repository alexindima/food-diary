using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Commands.SendAdminEmailTemplateTest;

public sealed class SendAdminEmailTemplateTestCommandValidator : AbstractValidator<SendAdminEmailTemplateTestCommand> {
    public SendAdminEmailTemplateTestCommandValidator() {
        RuleFor(x => x.ToEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.HtmlBody)
            .NotEmpty();

        RuleFor(x => x.TextBody)
            .NotEmpty();
    }
}
