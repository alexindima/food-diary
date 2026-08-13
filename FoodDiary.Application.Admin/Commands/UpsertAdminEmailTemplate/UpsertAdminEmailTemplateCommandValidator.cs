using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed class UpsertAdminEmailTemplateCommandValidator : AbstractValidator<UpsertAdminEmailTemplateCommand> {
    public UpsertAdminEmailTemplateCommandValidator() {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Locale)
            .NotEmpty()
            .Must(locale => LanguageCode.TryParse(locale, out _))
            .WithMessage("Locale must be one of the supported codes.");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.HtmlBody)
            .NotEmpty();

        RuleFor(x => x.TextBody)
            .NotEmpty();
    }
}
