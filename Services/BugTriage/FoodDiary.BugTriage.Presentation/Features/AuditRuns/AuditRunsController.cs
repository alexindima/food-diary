using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.AuditRuns;
using FoodDiary.BugTriage.Presentation.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.BugTriage.Presentation.Features.AuditRuns;

[ApiController]
[Route("api/audit-runs")]
[ServiceFilter(typeof(BugTriageAuthorizationFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("bugtriage")]
[RequestSizeLimit(65536)]
public sealed class AuditRunsController(IAuditRunStore store, TimeProvider timeProvider) : ControllerBase {
    [HttpPut("{id:guid}/start")]
    public async Task<IActionResult> StartAsync(Guid id, StartAuditRunHttpRequest request, CancellationToken cancellationToken) {
        DateTimeOffset now = timeProvider.GetUtcNow();
        var window = new AuditWindow(request.FromUtc, request.ToUtc);
        if (id == Guid.Empty || !window.IsValid(now)) {
            return BadRequest("A valid audit ID and bounded UTC interval are required.");
        }
        return await store.StartAsync((AuditRunId)id, window, now, cancellationToken)
            ? NoContent() : Conflict();
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> CompleteAsync(Guid id, CompleteAuditRunHttpRequest request, CancellationToken cancellationToken) {
        var completion = new AuditCompletion(request.Outcome, request.Summary, request.Findings);
        if (id == Guid.Empty || !completion.IsValid()) {
            return BadRequest("A bounded audit outcome, summary and findings are required.");
        }
        return await store.CompleteAsync((AuditRunId)id, completion, timeProvider.GetUtcNow(), cancellationToken)
            ? NoContent() : Conflict();
    }
}
