using System.Globalization;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Usda {
        public static Error FoodNotFound(int fdcId) => new(
            "Usda.FoodNotFound",
            $"USDA food with FDC ID {fdcId.ToString(CultureInfo.InvariantCulture)} was not found.",
            Kind: ErrorKind.NotFound);
    }
}
