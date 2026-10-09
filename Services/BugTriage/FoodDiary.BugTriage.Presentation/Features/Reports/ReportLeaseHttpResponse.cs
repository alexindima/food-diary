using FoodDiary.BugTriage.Application.Reports;

namespace FoodDiary.BugTriage.Presentation.Features.Reports;

public sealed record ReportLeaseHttpResponse(Guid Id, Guid SourceMessageId, string Subject, string TextBody,
    Guid LeaseToken, DateTimeOffset LeaseExpiresAtUtc, int Attempt, DateTimeOffset ContentExpiresAtUtc) {
    public static ReportLeaseHttpResponse FromLease(ReportLease lease) => new(
        lease.Id.Value, lease.SourceMessageId.Value, lease.Subject, lease.TextBody,
        lease.LeaseToken.Value, lease.LeaseExpiresAtUtc, lease.Attempt, lease.ContentExpiresAtUtc);
}
