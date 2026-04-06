using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Social;

public sealed class ContentReport : AggregateRoot<ContentReportId> {
    private const int ReasonMaxLength = 1000;
    private const int AdminNoteMaxLength = 2000;

    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;
    public ReportTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public string? AdminNote { get; private set; }
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

        var normalizedReason = NormalizeReason(reason);

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

    public void MarkReviewed(string? adminNote) {
        Status = ReportStatus.Reviewed;
        AdminNote = adminNote?.Trim();
        ReviewedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    public void MarkDismissed(string? adminNote) {
        Status = ReportStatus.Dismissed;
        AdminNote = adminNote?.Trim();
        ReviewedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    private static string NormalizeReason(string reason) {
        if (string.IsNullOrWhiteSpace(reason)) {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        var normalized = reason.Trim();
        return normalized.Length > ReasonMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(reason), $"Reason must be at most {ReasonMaxLength} characters.")
            : normalized;
    }
}
