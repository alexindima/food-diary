using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Meals.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Meal {
        public static Error NotFound(Guid id) => MealErrors.NotFound(id);

        public static Error InvalidData(string message) => MealErrors.InvalidData(message);
    }
}
