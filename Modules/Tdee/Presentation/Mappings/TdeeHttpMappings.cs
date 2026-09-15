using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;

namespace FoodDiary.Modules.Tdee.Presentation.Mappings;

public static class TdeeHttpMappings {
    extension(Guid userId) {
        public GetTdeeInsightQuery ToTdeeQuery() => new(userId);
    }
}
