using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelaySuppressions;

public sealed class GetMailRelaySuppressionsQueryValidator : AbstractValidator<GetMailRelaySuppressionsQuery> {
    public GetMailRelaySuppressionsQueryValidator() {
        RuleFor(static query => query.Page).InclusiveBetween(1, 10_000).WithErrorCode("Validation.Invalid");
        RuleFor(static query => query.Limit).InclusiveBetween(1, 100).WithErrorCode("Validation.Invalid");
        RuleFor(static query => query.Email)
            .EmailAddress()
            .When(static query => !string.IsNullOrWhiteSpace(query.Email))
            .WithErrorCode("Validation.Invalid");
    }
}
