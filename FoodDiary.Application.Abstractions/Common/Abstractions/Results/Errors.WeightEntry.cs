using System.Globalization;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class WeightEntry {
        public static Error NotFound(Guid id) => new(
            "WeightEntry.NotFound",
            $"Weight entry with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists(DateTime date) => new(
            "WeightEntry.AlreadyExists",
            string.Create(CultureInfo.InvariantCulture, $"Weight entry for {date:yyyy-MM-dd} already exists."),
            Kind: ErrorKind.Conflict);
    }
}
