using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Presentation.Options;
using FoodDiary.BugTriage.Presentation.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FoodDiary.BugTriage.Presentation.Features.Reports;

[ApiController]
[Route("api/bug-reports")]
[ServiceFilter(typeof(BugTriageAuthorizationFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("bugtriage")]
[RequestSizeLimit(65536)]
public sealed class BugReportsController(IBugReportStore store, TimeProvider timeProvider,
    IOptions<BugTriageHttpOptions> options) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> GetRecentAsync(CancellationToken cancellationToken) =>
        Ok(await store.GetRecentAsync(timeProvider.GetUtcNow(), cancellationToken));

    [HttpPost("claim")]
    public async Task<IActionResult> ClaimAsync(CancellationToken cancellationToken) {
        ReportLease? lease = await store.ClaimAsync(timeProvider.GetUtcNow(), options.Value.LeaseDuration,
            options.Value.MaxAttempts, cancellationToken);
        return lease is null ? NoContent() : Ok(lease);
    }

    [HttpPost("{id:guid}/renew")]
    public async Task<IActionResult> RenewAsync(Guid id, RenewReportHttpRequest request, CancellationToken cancellationToken) =>
        await store.RenewAsync(id, request.LeaseToken, timeProvider.GetUtcNow(), options.Value.LeaseDuration, cancellationToken)
            ? NoContent() : Conflict();

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteAsync(Guid id, CompleteReportHttpRequest request, CancellationToken cancellationToken) {
        var completion = new ReportCompletion(request.Outcome, request.Summary, request.MergeRequestUrl);
        if (request.LeaseToken == Guid.Empty || !completion.IsValid()) {
            return BadRequest("A valid lease, outcome and summary are required; draft_ready requires an HTTPS MR URL.");
        }
        return await store.CompleteAsync(id, request.LeaseToken, completion, timeProvider.GetUtcNow(), cancellationToken)
            ? NoContent() : Conflict();
    }

    [HttpGet("{id:guid}/mime")]
    public async Task<IActionResult> GetMimeAsync(Guid id, [FromHeader(Name = "X-BugTriage-Lease")] Guid leaseToken,
        CancellationToken cancellationToken) {
        byte[]? mime = await store.GetMimeAsync(id, leaseToken, timeProvider.GetUtcNow(), cancellationToken);
        return mime is null ? NotFound() : File(mime, "application/octet-stream", $"{id}.eml");
    }
}
