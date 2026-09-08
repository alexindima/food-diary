using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminRetentionReader {
    Task<AdminRetentionReport> GetAsync(DateTime fromUtc, DateTime toUtc, DateTime asOfUtc, CancellationToken cancellationToken);
}
