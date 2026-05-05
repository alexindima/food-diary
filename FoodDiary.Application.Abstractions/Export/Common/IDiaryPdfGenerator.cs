using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Abstractions.Export.Common;

public interface IDiaryPdfGenerator {
    Task<byte[]> GenerateAsync(
        IReadOnlyList<Meal> meals,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken);
}
