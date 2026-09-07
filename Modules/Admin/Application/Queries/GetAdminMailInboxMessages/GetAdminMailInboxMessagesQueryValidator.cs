using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed class GetAdminMailInboxMessagesQueryValidator : AbstractValidator<GetAdminMailInboxMessagesQuery> {
    public GetAdminMailInboxMessagesQueryValidator() {
        RuleFor(static query => query.Recipient).MaximumLength(320);
        RuleFor(static query => query.Category).Must(static category => category is null or "general" or "dmarc-report");
        RuleFor(static query => query.Limit)
            .InclusiveBetween(1, 200);
    }
}
