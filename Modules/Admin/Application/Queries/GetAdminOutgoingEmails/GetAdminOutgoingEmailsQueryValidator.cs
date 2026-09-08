using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminOutgoingEmails;

public sealed class GetAdminOutgoingEmailsQueryValidator : AbstractValidator<GetAdminOutgoingEmailsQuery> {
    public GetAdminOutgoingEmailsQueryValidator() {
        RuleFor(x => x.CorrelationId).MaximumLength(256);
        RuleFor(x => x).Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.FromUtc < x.ToUtc).WithMessage("The start date must precede the exclusive end date.");
        RuleFor(x => x.Page).InclusiveBetween(1, 10000);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.Purpose).MaximumLength(64);
        RuleFor(x => x.Status).MaximumLength(32);
        RuleFor(x => x.Recipient).MaximumLength(320);
    }
}
