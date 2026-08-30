using System.Globalization;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.WaistEntries.Common;

public static class WaistEntryErrors {
    public static Error NotFound(Guid id) => new(
        "WaistEntry.NotFound",
        $"Waist entry with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error NotAccessible(Guid id) => new(
        "WaistEntry.NotAccessible",
        $"Waist entry with ID {id} was not found or is not accessible.",
        Kind: ErrorKind.NotFound);

    public static Error AlreadyExists(DateTime date) => new(
        "WaistEntry.AlreadyExists",
        string.Create(CultureInfo.InvariantCulture, $"Waist entry for {date:yyyy-MM-dd} already exists."),
        Kind: ErrorKind.Conflict);
}
