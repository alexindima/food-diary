using FoodDiary.Results;

using FoodDiary.Application.Abstractions.FavoriteMeals.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class FavoriteMeal {
        public static Error NotFound(Guid id) => FavoriteMealErrors.NotFound(id);

        public static Error AlreadyExists => FavoriteMealErrors.AlreadyExists;
    }
}
