using FoodDiary.Application.Abstractions.Products.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Product {
        public static Error NotFound(Guid id) => ProductErrors.NotFound(id);

        public static Error NotAccessible(Guid id) => ProductErrors.NotAccessible(id);

        public static Error AlreadyExists(string barcode) => ProductErrors.AlreadyExists(barcode);

        public static Error InvalidData(string message) => ProductErrors.InvalidData(message);
    }
}
