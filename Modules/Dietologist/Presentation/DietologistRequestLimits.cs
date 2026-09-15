using FoodDiary.Modules.Dietologist.Application.Commands.SetAttentionSignalState;
using FoodDiary.Modules.Dietologist.Application.Queries.SearchRecommendationTemplates;

namespace FoodDiary.Modules.Dietologist.Presentation;

public static class DietologistRequestLimits {
    public const int MaximumTemplateSearchLength = SearchRecommendationTemplatesQueryValidator.MaximumSearchLength;
    public const int MaximumSignalIdLength = SetAttentionSignalStateCommandValidator.MaximumSignalIdLength;
}
