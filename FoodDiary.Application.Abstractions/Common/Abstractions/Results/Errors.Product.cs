namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Product {
        public static Error NotFound(Guid id) => new(
            "Product.NotFound",
            $"Product with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error NotAccessible(Guid id) => new(
            "Product.NotAccessible",
            $"Product with ID {id} does not belong to the current user or was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists(string barcode) => new(
            "Product.AlreadyExists",
            $"Product with barcode {barcode} already exists.",
            Kind: ErrorKind.Conflict);

        public static Error InvalidData(string message) => new(
            "Product.InvalidData",
            message,
            Kind: ErrorKind.Internal);
    }
}
