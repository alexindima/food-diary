using FoodDiary.Application.Abstractions.Meals.Models;

namespace FoodDiary.Application.Abstractions.Export.Common;

public interface IDiaryPdfGenerator {
    Task<byte[]> GenerateAsync(
        IReadOnlyList<MealConsumptionReadModel> meals,
        DateTime dateFrom,
        DateTime dateTo,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin,
        CancellationToken cancellationToken);
}
