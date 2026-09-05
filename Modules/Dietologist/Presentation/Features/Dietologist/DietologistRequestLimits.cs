using FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;
using FoodDiary.Application.Dietologist.Queries.SearchRecommendationTemplates;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

public static class DietologistRequestLimits {
    public const int MaximumTemplateSearchLength = SearchRecommendationTemplatesQueryValidator.MaximumSearchLength;
    public const int MaximumSignalIdLength = SetAttentionSignalStateCommandValidator.MaximumSignalIdLength;
}
