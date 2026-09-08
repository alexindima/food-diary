using FluentValidation;

namespace FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessagePage;

public sealed class GetInboundMailMessagePageQueryValidator : AbstractValidator<GetInboundMailMessagePageQuery> {
    public GetInboundMailMessagePageQueryValidator() {
        RuleFor(static query => query.Page).GreaterThan(0);
        RuleFor(static query => query.Limit).InclusiveBetween(1, 200);
        RuleFor(static query => query.Recipient).MaximumLength(320);
        RuleFor(static query => query.Category).Must(static category => category is null or "general" or "dmarc-report");
    }
}

