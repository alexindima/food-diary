using FoodDiary.Application.Abstractions.MealPlans.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class MealPlan {
        public static Error NotFound(Guid id) => MealPlanErrors.NotFound(id);

        public static Error InvalidId => MealPlanErrors.InvalidId;

        public static Error NotCurated => MealPlanErrors.NotCurated;
    }
}
