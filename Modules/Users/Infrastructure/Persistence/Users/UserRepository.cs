using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserRepository(FoodDiaryDbContext context) : IUserRepository, IUserGoogleIdentityRepository {
    private IQueryable<User> UsersWithRoles() =>
        context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role);

    private IQueryable<User> UsersWithRolesAndGoals() =>
        UsersWithRoles()
            .Include(u => u.WeightGoals)
            .Include(u => u.WaistGoals);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Email == email && u.IsActive && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByGoogleIdentityIncludingDeletedAsync(string issuer, string subject, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.GoogleIssuer == issuer && u.GoogleSubject == subject, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        await UsersWithRolesAndGoals().FirstOrDefaultAsync(u =>
            u.Id == id && u.IsActive && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.TelegramUserId == telegramUserId && u.IsActive && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken).ConfigureAwait(false);

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
        await context.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
        TrackForUpdate(user);
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) {
        TrackForUpdate(user);
        if (roleAuditEvents.Count > 0) {
            await context.UserRoleAuditEvents.AddRangeAsync(roleAuditEvents, cancellationToken).ConfigureAwait(false);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void TrackForUpdate(User user) {
        if (context.Entry(user).State == EntityState.Detached) {
            context.Users.Update(user);
        }
    }

}
