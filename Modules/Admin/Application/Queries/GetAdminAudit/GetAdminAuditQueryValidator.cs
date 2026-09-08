using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminAudit;

public sealed class GetAdminAuditQueryValidator : AbstractValidator<GetAdminAuditQuery> {
    public GetAdminAuditQueryValidator() {
        RuleFor(query => query.Filter.Page).InclusiveBetween(1, 10000);
        RuleFor(query => query.Filter.Limit).InclusiveBetween(1, 100);
        RuleFor(query => query.Filter.Action).MaximumLength(200);
        RuleFor(query => query.Filter.TargetType).MaximumLength(100);
        RuleFor(query => query.Filter.TargetId).MaximumLength(200);
        RuleFor(query => query.Filter).Must(filter => !filter.FromUtc.HasValue || !filter.ToUtc.HasValue || filter.FromUtc < filter.ToUtc)
            .WithMessage("The start must precede the exclusive end.");
    }
}
