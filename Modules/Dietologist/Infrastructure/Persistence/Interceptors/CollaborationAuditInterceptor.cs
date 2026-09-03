using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.Persistence.Interceptors;

internal sealed class CollaborationAuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor {
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result) {
        AddEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) {
        AddEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddEntries(DbContext? context) {
        if (context is null) {
            return;
        }

        DateTime timestamp = timeProvider.GetUtcNow().UtcDateTime;
        AuditEntry[] entries = [
            .. context.ChangeTracker.Entries()
                .Where(entry => entry.Entity is not AuditEntry)
                .Select(entry => CreateEntry(entry, timestamp))
                .Where(entry => entry is not null)
                .Cast<AuditEntry>(),
        ];
        if (entries.Length > 0) {
            context.Set<AuditEntry>().AddRange(entries);
        }
    }

    private static AuditEntry? CreateEntry(EntityEntry entry, DateTime timestamp) =>
        entry.Entity switch {
            DietologistInvitation invitation => CreateInvitationEntry(entry, invitation, timestamp),
            Recommendation recommendation => CreateRecommendationEntry(entry, recommendation, timestamp),
            ClientTask task => CreateTaskEntry(entry, task, timestamp),
            RecommendationBulkDispatch dispatch when entry.State == EntityState.Added => NewEntry(
                dispatch.DietologistUserId.Value,
                dispatch.ClientUserId.Value,
                "dietologist.bulk-recipient.sent",
                "Recommendation",
                dispatch.RecommendationId.Value.ToString(),
                metadata: null,
                timestamp),
            _ => null,
        };

    private static AuditEntry? CreateInvitationEntry(
        EntityEntry entry,
        DietologistInvitation invitation,
        DateTime timestamp) {
        if (entry.State == EntityState.Added) {
            return NewEntry(
                invitation.ClientUserId.Value,
                invitation.ClientUserId.Value,
                "dietologist.invitation.created",
                "DietologistInvitation",
                invitation.Id.Value.ToString(),
                metadata: null,
                timestamp);
        }

        if (entry.State != EntityState.Modified) {
            return null;
        }

        if (entry.Property(nameof(DietologistInvitation.Status)).IsModified) {
            return CreateInvitationStatusEntry(invitation, timestamp);
        }

        string[] permissionNames = [
            nameof(DietologistInvitation.ShareProfile),
            nameof(DietologistInvitation.ShareMeals),
            nameof(DietologistInvitation.ShareStatistics),
            nameof(DietologistInvitation.ShareWeight),
            nameof(DietologistInvitation.ShareWaist),
            nameof(DietologistInvitation.ShareGoals),
            nameof(DietologistInvitation.ShareHydration),
            nameof(DietologistInvitation.ShareFasting),
        ];
        if (permissionNames.Any(name => entry.Property(name).IsModified)) {
            return NewEntry(
                invitation.ClientUserId.Value,
                invitation.ClientUserId.Value,
                "dietologist.permissions.updated",
                "DietologistInvitation",
                invitation.Id.Value.ToString(),
                metadata: null,
                timestamp);
        }

        return null;
    }

    private static AuditEntry? CreateInvitationStatusEntry(
        DietologistInvitation invitation,
        DateTime timestamp) =>
        invitation.Status switch {
            DietologistInvitationStatus.Accepted when invitation.DietologistUserId.HasValue => NewEntry(
                invitation.DietologistUserId.Value.Value,
                invitation.ClientUserId.Value,
                "dietologist.invitation.accepted",
                "DietologistInvitation",
                invitation.Id.Value.ToString(),
                """{"status":"Accepted"}""",
                timestamp),
            DietologistInvitationStatus.Declined => NewEntry(
                invitation.DietologistUserId?.Value ?? invitation.ClientUserId.Value,
                invitation.ClientUserId.Value,
                "dietologist.invitation.declined",
                "DietologistInvitation",
                invitation.Id.Value.ToString(),
                """{"status":"Declined"}""",
                timestamp),
            DietologistInvitationStatus.Revoked => NewEntry(
                invitation.ClientUserId.Value,
                invitation.ClientUserId.Value,
                "dietologist.relationship.disconnected",
                "DietologistInvitation",
                invitation.Id.Value.ToString(),
                """{"status":"Revoked"}""",
                timestamp),
            _ => null,
        };

    private static AuditEntry? CreateRecommendationEntry(
        EntityEntry entry,
        Recommendation recommendation,
        DateTime timestamp) {
        if (entry.State == EntityState.Added) {
            return NewEntry(
                recommendation.DietologistUserId.Value,
                recommendation.ClientUserId.Value,
                "dietologist.recommendation.created",
                "Recommendation",
                recommendation.Id.Value.ToString(),
                metadata: null,
                timestamp);
        }

        return entry.State == EntityState.Modified &&
               entry.Property(nameof(Recommendation.IsRead)).IsModified &&
               recommendation.IsRead
            ? NewEntry(
                recommendation.ClientUserId.Value,
                recommendation.ClientUserId.Value,
                "dietologist.recommendation.read",
                "Recommendation",
                recommendation.Id.Value.ToString(),
                metadata: null,
                timestamp)
            : null;
    }

    private static AuditEntry? CreateTaskEntry(EntityEntry entry, ClientTask task, DateTime timestamp) {
        if (entry.State == EntityState.Added) {
            return NewEntry(
                task.DietologistUserId.Value,
                task.ClientUserId.Value,
                "dietologist.task.created",
                "ClientTask",
                task.Id.Value.ToString(),
                """{"status":"Open"}""",
                timestamp);
        }

        if (entry.State != EntityState.Modified ||
            !entry.Property(nameof(ClientTask.Status)).IsModified) {
            return null;
        }

        bool isCancelled = task.Status == ClientTaskStatus.Cancelled;
        return NewEntry(
            isCancelled ? task.DietologistUserId.Value : task.ClientUserId.Value,
            task.ClientUserId.Value,
            isCancelled ? "dietologist.task.cancelled" : "dietologist.task.status-changed",
            "ClientTask",
            task.Id.Value.ToString(),
            $$"""{"status":"{{task.Status}}"}""",
            timestamp);
    }

    private static AuditEntry NewEntry(
        Guid actorUserId,
        Guid? subjectClientUserId,
        string action,
        string targetType,
        string? targetId,
        string? metadata,
        DateTime timestamp) =>
        new() {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            SubjectClientUserId = subjectClientUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Metadata = metadata,
            CreatedAtUtc = timestamp,
        };
}
