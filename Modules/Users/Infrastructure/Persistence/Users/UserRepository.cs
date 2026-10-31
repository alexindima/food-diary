using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserRepository(DbSet<User> users, DbSet<UserRoleAuditEvent> auditEvents, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserRepository, IUserGoogleIdentityRepository {
    private IQueryable<User> UsersWithRoles() =>
        users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role);

    private IQueryable<User> UsersWithRolesAndGoals() =>
        UsersWithRoles()
            .Include(u => u.WeightGoals)
            .Include(u => u.WaistGoals);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Email == email && u.IsActive && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRoles().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByGoogleIdentityIncludingDeletedAsync(string issuer, string subject, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.GoogleIssuer == issuer && u.GoogleSubject == subject, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRolesAndGoals().FirstOrDefaultAsync(u =>
            u.Id == id && u.IsActive && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.TelegramUserId == telegramUserId && u.IsActive && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await UsersWithRoles().FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await users.AddAsync(user, cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await TrackForUpdateAsync(user, cancellationToken);

    }

    public async Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await TrackForUpdateAsync(user, cancellationToken);
        if (roleAuditEvents.Count > 0) {
            await auditEvents.AddRangeAsync(roleAuditEvents, cancellationToken);
        }

        await Task.CompletedTask;
    }

    private async Task TrackForUpdateAsync(User user, CancellationToken cancellationToken) {
        if (users.Entry(user).State != EntityState.Detached) {
            return;
        }
        long? persistedRevision = user.NutritionProfile.GoalHistory.Count == 0 ? null : await users.AsNoTracking()
            .Where(account => account.Id == user.Id).Select(account => (long?)account.NutritionProfile.GoalRevision)
            .SingleOrDefaultAsync(cancellationToken);
        users.Update(user);
        foreach (UserGoalRevision revision in user.NutritionProfile.GoalHistory) {
            // Update attaches a detached graph as Modified even for newly appended, client-keyed snapshots.
            // Existing snapshots are immutable; only revisions added after the persisted profile are inserted.
            users.Entry(user).Reference(account => account.NutritionProfile).TargetEntry!
                .Collection(profile => profile.GoalHistory).FindEntry(revision)!.State = persistedRevision is null || revision.Revision > persistedRevision
                ? EntityState.Added : EntityState.Unchanged;
        }
    }

}
