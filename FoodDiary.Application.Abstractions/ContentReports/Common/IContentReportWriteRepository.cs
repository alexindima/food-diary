using FoodDiary.Domain.Entities.Social;

namespace FoodDiary.Application.Abstractions.ContentReports.Common;

public interface IContentReportWriteRepository : IContentReportReadRepository {
    Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default);

    Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default);
}
