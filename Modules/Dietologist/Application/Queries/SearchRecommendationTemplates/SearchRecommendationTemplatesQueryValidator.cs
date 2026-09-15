using FluentValidation;

namespace FoodDiary.Modules.Dietologist.Application.Queries.SearchRecommendationTemplates;

public sealed class SearchRecommendationTemplatesQueryValidator : AbstractValidator<SearchRecommendationTemplatesQuery> {
    public const int MaximumSearchLength = 256;

    public SearchRecommendationTemplatesQueryValidator() {
        RuleFor(query => query.UserId)
            .NotEmpty();
        RuleFor(query => query.Search)
            .MaximumLength(MaximumSearchLength);
    }
}
