using FoodDiary.Modules.Meals.Contracts.Models;

namespace FoodDiary.Modules.Export.Application.Abstractions.Common;

public interface IDiaryPdfGenerator {
    Task<byte[]> GenerateAsync(
        IReadOnlyList<MealProjectionReadModel> meals,
        DateTime dateFrom,
        DateTime dateTo,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin,
        CancellationToken cancellationToken);
}
