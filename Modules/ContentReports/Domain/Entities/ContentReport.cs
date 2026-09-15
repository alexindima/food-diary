using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Domain.Entities;

public sealed class ContentReport : AggregateRoot<ContentReportId> {
    private const int ReasonMaxLength = 1000;
    private const int AdminNoteMaxLength = 2000;

    public UserId UserId { get; private set; }
    public ReportTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public string? AdminNote { get; private set; }
    public UserId? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    private ContentReport() {
    }

    public static ContentReport Create(UserId userId, ReportTargetType targetType, Guid targetId, string reason) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (targetId == Guid.Empty) {
            throw new ArgumentException("TargetId is required.", nameof(targetId));
        }

        ContentReportsDomainGuard.Defined(targetType, nameof(targetType));

        string normalizedReason = NormalizeReason(reason);

        var report = new ContentReport {
            Id = ContentReportId.New(),
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            Reason = normalizedReason,
            Status = ReportStatus.Pending,
        };
        report.SetCreated();
        return report;
    }

    public void MarkReviewed(UserId reviewerUserId, string? adminNote) {
        EnsurePending(reviewerUserId);
        string? normalizedAdminNote = ContentReportsDomainGuard.OptionalText(adminNote, AdminNoteMaxLength, nameof(adminNote));

        Status = ReportStatus.Reviewed;
        AdminNote = normalizedAdminNote;
        ReviewedByUserId = reviewerUserId;
        ReviewedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    public void MarkDismissed(UserId reviewerUserId, string? adminNote) {
        EnsurePending(reviewerUserId);
        string? normalizedAdminNote = ContentReportsDomainGuard.OptionalText(adminNote, AdminNoteMaxLength, nameof(adminNote));

        Status = ReportStatus.Dismissed;
        AdminNote = normalizedAdminNote;
        ReviewedByUserId = reviewerUserId;
        ReviewedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    private void EnsurePending(UserId reviewerUserId) {
        if (reviewerUserId == UserId.Empty) {
            throw new ArgumentException("ReviewerUserId is required.", nameof(reviewerUserId));
        }

        if (Status != ReportStatus.Pending) {
            throw new InvalidOperationException("Only pending content reports can be resolved.");
        }
    }

    private static string NormalizeReason(string reason) {
        if (string.IsNullOrWhiteSpace(reason)) {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        string normalized = reason.Trim();
        return normalized.Length > ReasonMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(reason), $"Reason must be at most {ReasonMaxLength} characters.")
            : normalized;
    }
}
