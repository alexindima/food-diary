using System.Globalization;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.WeightEntries.Common;

public static class WeightEntryErrors {
    public static Error NotFound(Guid id) => new(
        "WeightEntry.NotFound",
        $"Weight entry with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error NotAccessible(Guid id) => new(
        "WeightEntry.NotAccessible",
        $"Weight entry with ID {id} was not found or is not accessible.",
        Kind: ErrorKind.NotFound);

    public static Error AlreadyExists(DateTime date) => new(
        "WeightEntry.AlreadyExists",
        string.Create(CultureInfo.InvariantCulture, $"Weight entry for {date:yyyy-MM-dd} already exists."),
        Kind: ErrorKind.Conflict);
}
