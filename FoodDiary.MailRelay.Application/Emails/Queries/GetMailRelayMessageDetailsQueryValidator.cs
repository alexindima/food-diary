using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayMessageDetailsQueryValidator : AbstractValidator<GetMailRelayMessageDetailsQuery> {
    public GetMailRelayMessageDetailsQueryValidator() {
        RuleFor(static query => query.Id)
            .NotEmpty().WithErrorCode("Validation.Required");
    }
}
