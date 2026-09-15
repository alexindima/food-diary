using FoodDiary.Modules.Dietologist.Infrastructure.Persistence.Interceptors;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects;
using FoodDiary.Modules.Dietologist.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class CollaborationAuditInterceptorTests {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SavingChangesAsync_AddedCollaborationEntities_CreatesAuditEntries() {
        await using FoodDiaryDbContext context = CreateContext();
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateInvitation(clientId);
        var recommendation = Recommendation.Create(dietologistId, clientId, "Recommendation");
        var task = ClientTask.Create(
            dietologistId,
            clientId,
            "Task",
            details: null,
            dueAtUtc: UtcNow.AddDays(1));
        var dispatch = RecommendationBulkDispatch.Create(
            dietologistId,
            clientId,
            recommendation.Id,
            "key");
        context.AddRange(invitation, recommendation, task, dispatch);

        await context.SaveChangesAsync();

        string[] actions = [.. context.AuditEntries.Select(entry => entry.Action)];
        Assert.Multiple(
            () => Assert.Contains("dietologist.invitation.created", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.recommendation.created", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.task.created", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.bulk-recipient.sent", actions, StringComparer.Ordinal));
    }

    [Fact]
    public void SavingChanges_NullContextAndUnrelatedEntity_DoNotCreateAuditEntries() {
        var interceptor = new CollaborationAuditInterceptor(new FixedTimeProvider());
        MethodInfo addEntries = typeof(CollaborationAuditInterceptor).GetMethod(
            "AddEntries",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        addEntries.Invoke(interceptor, [null]);

        using FoodDiaryDbContext context = CreateContext();
        context.Users.Add(User.Create("audit-unrelated@example.com", "hash"));
        context.SaveChanges();
        Assert.Empty(context.AuditEntries);
    }

    [Fact]
    public void SavingChanges_ModifiedInvitationWithoutAuditedChangeAndPendingStatus_DoNotCreateEntries() {
        using FoodDiaryDbContext context = CreateContext();
        DietologistInvitation unrelatedChange = CreateInvitation(UserId.New());
        DietologistInvitation pendingStatus = CreateInvitation(UserId.New());
        context.AddRange(unrelatedChange, pendingStatus);
        context.SaveChanges();
        context.AuditEntries.RemoveRange(context.AuditEntries);
        context.SaveChanges();
        context.Entry(unrelatedChange).Property(nameof(DietologistInvitation.ExpiresAtUtc)).IsModified = true;
        context.Entry(pendingStatus).Property(nameof(DietologistInvitation.Status)).IsModified = true;

        context.SaveChanges();

        Assert.Empty(context.AuditEntries);
    }

    [Fact]
    public void SavingChanges_ModifiedCollaborationEntities_CreatesStatusAuditEntries() {
        using FoodDiaryDbContext context = CreateContext();
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation accepted = CreateInvitation(clientId);
        DietologistInvitation declined = CreateInvitation(clientId);
        DietologistInvitation revoked = CreateInvitation(clientId);
        DietologistInvitation permissions = CreateInvitation(clientId);
        var recommendation = Recommendation.Create(dietologistId, clientId, "Recommendation");
        var cancelledTask = ClientTask.Create(
            dietologistId,
            clientId,
            "Cancelled",
            details: null,
            dueAtUtc: null);
        var completedTask = ClientTask.Create(
            dietologistId,
            clientId,
            "Completed",
            details: null,
            dueAtUtc: null);
        context.AddRange(accepted, declined, revoked, permissions, recommendation, cancelledTask, completedTask);
        context.SaveChanges();
        context.AuditEntries.RemoveRange(context.AuditEntries);
        context.SaveChanges();

        accepted.Accept(dietologistId);
        declined.Decline();
        revoked.Revoke();
        permissions.UpdatePermissions(new DietologistPermissions(ShareMeals: false));
        recommendation.MarkAsRead();
        cancelledTask.Cancel();
        completedTask.Complete();
        context.SaveChanges();

        string[] actions = [.. context.AuditEntries.Select(entry => entry.Action)];
        Assert.Multiple(
            () => Assert.Contains("dietologist.invitation.accepted", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.invitation.declined", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.relationship.disconnected", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.permissions.updated", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.recommendation.read", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.task.cancelled", actions, StringComparer.Ordinal),
            () => Assert.Contains("dietologist.task.status-changed", actions, StringComparer.Ordinal));
    }

    [Fact]
    public async Task CoordinatedAudit_WithNonRelationalProvider_RejectsBeforePersistenceAsync() {
        await using FoodDiaryDbContext context = CreateContext();
        await using DietologistDbContext owned = context.CreateModuleContext<DietologistDbContext>(static options => new DietologistDbContext(options));
        owned.Recommendations.Add(Recommendation.Create(UserId.New(), UserId.New(), "Advice"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => ModuleContextSaveCoordinator.SaveAsync(context.Session, NullLogger.Instance));

        Assert.Empty(await context.AuditEntries.AsNoTracking().ToListAsync());
        Assert.Empty(await owned.Recommendations.AsNoTracking().ToListAsync());
    }

    private static DietologistInvitation CreateInvitation(UserId clientId) =>
        DietologistInvitation.Create(
            clientId,
            $"{Guid.NewGuid():N}@example.com",
            "hash",
            DateTime.UtcNow.AddDays(1),
            DietologistPermissions.AllEnabled);

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new CollaborationAuditInterceptor(new FixedTimeProvider()))
            .Options;
        return new FoodDiaryDbContext(options);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }
}
