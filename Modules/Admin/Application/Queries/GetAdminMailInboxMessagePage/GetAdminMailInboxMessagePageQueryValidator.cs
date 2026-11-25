using System.Net.Mail;
using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessagePage;

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
        RuleFor(static query => query.ExcludeRecipients).Must(static recipients => recipients is null or { Count: <= 10 })
            .WithMessage("At most 10 recipient addresses can be excluded.")
            .DependentRules(() => RuleForEach(static query => query.ExcludeRecipients)
                .Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(320)
                .Must(static address => MailAddress.TryCreate(address.Trim(), out MailAddress? parsed) &&
                    parsed.Address.Equals(address.Trim(), StringComparison.Ordinal))
                .WithMessage("Each excluded recipient must be an email address."));
    }
}

