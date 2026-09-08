using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessagePage;

public sealed class GetAdminMailInboxMessagePageQueryValidator : AbstractValidator<GetAdminMailInboxMessagePageQuery> {
    public GetAdminMailInboxMessagePageQueryValidator() {
        RuleFor(static query => query.Page).GreaterThan(0);
        RuleFor(static query => query.Limit).InclusiveBetween(1, 200);
        RuleFor(static query => query.Recipient).MaximumLength(320);
        RuleFor(static query => query.Category).Must(static category => category is null or "general" or "dmarc-report");
    }
}

