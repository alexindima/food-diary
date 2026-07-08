using FoodDiary.Results;

using FoodDiary.Application.Abstractions.FavoriteProducts.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class FavoriteProduct {
        public static Error NotFound(Guid id) => FavoriteProductErrors.NotFound(id);

        public static Error AlreadyExists => FavoriteProductErrors.AlreadyExists;
    }
}
