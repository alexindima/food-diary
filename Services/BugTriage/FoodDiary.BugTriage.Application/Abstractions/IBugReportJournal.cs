using FoodDiary.BugTriage.Application.Reports;

namespace FoodDiary.BugTriage.Application.Abstractions;

public interface IBugReportJournal {
    Task<BugReportJournalPage> GetPageAsync(BugReportJournalFilter filter, DateTimeOffset now, CancellationToken cancellationToken);
}
