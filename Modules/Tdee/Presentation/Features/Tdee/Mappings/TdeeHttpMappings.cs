using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

namespace FoodDiary.Presentation.Api.Features.Tdee.Mappings;

public static class TdeeHttpMappings {
    extension(Guid userId) {
        public GetTdeeInsightQuery ToTdeeQuery() => new(userId);
    }
}
