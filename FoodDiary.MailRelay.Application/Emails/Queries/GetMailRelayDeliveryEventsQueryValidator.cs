using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayDeliveryEventsQueryValidator : AbstractValidator<GetMailRelayDeliveryEventsQuery> {
    public GetMailRelayDeliveryEventsQueryValidator() {
        RuleFor(static query => query.Email)
            .EmailAddress()
            .When(static query => !string.IsNullOrWhiteSpace(query.Email))
            .WithErrorCode("Validation.Invalid");
    }
}
