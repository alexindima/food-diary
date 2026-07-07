using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Consumptions.Common;

public static class ConsumptionErrors {
    public static Error NotFound(Guid id) => new(
        "Consumption.NotFound",
        $"Consumption with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidData(string message) => new(
        "Consumption.InvalidData",
        message,
        Kind: ErrorKind.Internal);
}
