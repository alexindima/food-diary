using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessagePage;

public sealed class GetAdminMailInboxMessagePageQueryValidator : AbstractValidator<GetAdminMailInboxMessagePageQuery> {
    public GetAdminMailInboxMessagePageQueryValidator() {
        RuleFor(query => query.Search).MaximumLength(320);
        RuleFor(query => query.FromAddress).MaximumLength(320);
        RuleFor(query => query).Must(query => !query.FromUtc.HasValue || !query.ToUtc.HasValue || query.FromUtc < query.ToUtc)
            .WithMessage("The start must precede the exclusive end.");
        RuleFor(static query => query.Page).GreaterThan(0);
        RuleFor(static query => query.Limit).InclusiveBetween(1, 200);
        RuleFor(static query => query.Recipient).MaximumLength(320);
        RuleFor(static query => query.Category).Must(static category => category is null or "general" or "dmarc-report");
    }
}

