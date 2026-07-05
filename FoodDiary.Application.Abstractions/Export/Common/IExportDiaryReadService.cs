using FoodDiary.Application.Abstractions.Export.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Export.Common;

public interface IExportDiaryReadService {
    Task<ExportDiaryMealsReadModel> GetMealsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken);
}
