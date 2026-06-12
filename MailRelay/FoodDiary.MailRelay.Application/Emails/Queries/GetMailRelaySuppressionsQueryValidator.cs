using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelaySuppressionsQueryValidator : AbstractValidator<GetMailRelaySuppressionsQuery> {
    public GetMailRelaySuppressionsQueryValidator() {
        RuleFor(static query => query.Email)
            .EmailAddress()
            .When(static query => !string.IsNullOrWhiteSpace(query.Email))
            .WithErrorCode("Validation.Invalid");
    }
}
