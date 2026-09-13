using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminRetentionReader {
    Task<AdminRetentionReport> GetAsync(DateTime fromUtc, DateTime toUtc, DateTime asOfUtc, CancellationToken cancellationToken);
}
