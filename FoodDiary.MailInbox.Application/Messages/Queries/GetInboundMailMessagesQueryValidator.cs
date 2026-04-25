using FluentValidation;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed class GetInboundMailMessagesQueryValidator : AbstractValidator<GetInboundMailMessagesQuery> {
    public GetInboundMailMessagesQueryValidator() {
        RuleFor(static query => query.Limit)
            .InclusiveBetween(1, 200);
    }
}
