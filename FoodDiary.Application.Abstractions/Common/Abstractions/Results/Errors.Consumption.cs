namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Consumption {
        public static Error NotFound(Guid id) => new(
            "Consumption.NotFound",
            $"Consumption with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidData(string message) => new(
            "Consumption.InvalidData",
            message,
            Kind: ErrorKind.Internal);
    }
}
