namespace FoodDiary.Infrastructure.Persistence.Audit;

internal sealed class AuditEntry {
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? SubjectClientUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
