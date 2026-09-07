using FluentValidation;

namespace FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessages;

public sealed class GetInboundMailMessagesQueryValidator : AbstractValidator<GetInboundMailMessagesQuery> {
    public GetInboundMailMessagesQueryValidator() {
        RuleFor(static query => query.Recipient).MaximumLength(320);
        RuleFor(static query => query.Category).Must(static category => category is null or "general" or "dmarc-report");
        RuleFor(static query => query.Limit)
            .InclusiveBetween(1, 200);
    }
}
