using FoodDiary.BugTriage.Application.Reports;

namespace FoodDiary.BugTriage.Application.Abstractions;

public interface IBugMailSource {
    IAsyncEnumerable<ImportedReport> ReadNewAsync(CancellationToken cancellationToken);
}
