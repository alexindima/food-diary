using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public static class UsdaErrors {
    public static Error FoodNotFound(int fdcId) => new(
        "Usda.FoodNotFound",
        $"USDA food with FDC ID {fdcId.ToString(CultureInfo.InvariantCulture)} was not found.",
        Kind: ErrorKind.NotFound);
}
