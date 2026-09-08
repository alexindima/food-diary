using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Presentation.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.BugTriage.Presentation.Features.Reports;

[ApiController]
[Route("api/report-journal")]
[ServiceFilter(typeof(BugTriageReadAuthorizationFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("bugtriage")]
public sealed class BugReportJournalController(IBugReportJournal journal, TimeProvider timeProvider) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<BugReportJournalPage>> GetPageAsync([FromQuery] BugReportJournalHttpQuery query, CancellationToken cancellationToken) =>
        Ok(await journal.GetPageAsync(new BugReportJournalFilter(query.Page, query.Limit, query.FromUtc, query.ToUtc, query.Status, query.Search, query.Id),
            timeProvider.GetUtcNow(), cancellationToken));
}
