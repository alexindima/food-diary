using System.Globalization;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class WaistEntry {
        public static Error NotFound(Guid id) => new(
            "WaistEntry.NotFound",
            $"Waist entry with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists(DateTime date) => new(
            "WaistEntry.AlreadyExists",
            string.Create(CultureInfo.InvariantCulture, $"Waist entry for {date:yyyy-MM-dd} already exists."),
            Kind: ErrorKind.Conflict);
    }
}
