using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Usda.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Usda {
        public static Error FoodNotFound(int fdcId) => UsdaErrors.FoodNotFound(fdcId);
    }
}
