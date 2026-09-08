using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminTemplateRevisions;

public sealed class GetAdminTemplateRevisionsQueryValidator : AbstractValidator<GetAdminTemplateRevisionsQuery> {
    public GetAdminTemplateRevisionsQueryValidator() {
        RuleFor(query => query.Key).NotEmpty().MaximumLength(64);
        RuleFor(query => query.Locale).NotEmpty().MaximumLength(10);
    }
}
